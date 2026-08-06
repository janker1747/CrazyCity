using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MiniGameSceneManager : MonoBehaviour
{
    private sealed class GameEntry
    {
        public MiniGameId Id;
        public GameObject Panel;
        public IMiniGameController Controller;
    }

    [Header("Rules")]
    [SerializeField, Min(1)] private int coinReward = 50;
    [SerializeField, Min(1)] private int scorePenalty = 50;
    [SerializeField, Min(0.1f)] private float minTimeLimitSeconds = 5f;
    [SerializeField, Min(0.1f)] private float maxTimeLimitSeconds = 10f;

    [Header("Rewards")]
    [SerializeField] private MiniGameRewardCatalog rewardCatalog;
    [SerializeField] private MiniGameRewardPanelView rewardPanelView;

    private readonly List<GameEntry> entries = new List<GameEntry>();
    private readonly List<Cargo> awardedCargo = new List<Cargo>();
    private readonly Cargo[] awardedCargoByCategory = new Cargo[3];

    private GameEntry activeEntry;
    private Player player;
    private TaskCompletionSource<MiniGameResult> completionSource;
    private MiniGameResult pendingResult;
    private float remainingTime;
    private bool resultShown;

    public bool IsRunning => activeEntry != null && !resultShown;

    private void Awake()
    {
        if (rewardCatalog == null)
            rewardCatalog = Resources.Load<MiniGameRewardCatalog>("MiniGameRewardCatalog");

        if (rewardPanelView == null)
            rewardPanelView = GetComponentInChildren<MiniGameRewardPanelView>(true);

        DiscoverControllers();
        SetAllGamePanelsActive(false);

        if (rewardPanelView == null)
        {
            Debug.LogError("RewardPanel view is not assigned.", this);
            enabled = false;
            return;
        }

        rewardPanelView.ContinueRequested += CloseResult;
        rewardPanelView.Hide();
    }

    private void Update()
    {
        if (!IsRunning)
            return;

        remainingTime -= Time.unscaledDeltaTime;

        if (remainingTime <= 0f)
            FinishGame(false);
    }

    public Task<MiniGameResult> PlayAsync(MiniGameId game, Player owner)
    {
        if (completionSource != null)
            throw new InvalidOperationException("This manager is already running a mini-game.");

        if (owner == null)
            throw new ArgumentNullException(nameof(owner));

        GameEntry entry = entries.Find(item => item.Id == game);
        if (entry == null)
            throw new InvalidOperationException($"Panel for mini-game '{game}' was not found.");

        player = owner;
        activeEntry = entry;
        completionSource = new TaskCompletionSource<MiniGameResult>();
        remainingTime = UnityEngine.Random.Range(
            Mathf.Min(minTimeLimitSeconds, maxTimeLimitSeconds),
            Mathf.Max(minTimeLimitSeconds, maxTimeLimitSeconds));
        resultShown = false;

        SetAllGamePanelsActive(false);
        rewardPanelView.Hide();

        activeEntry.Controller.Finished += OnMiniGameFinished;
        activeEntry.Panel.SetActive(true);
        activeEntry.Controller.Begin();

        return completionSource.Task;
    }

    /// <summary>Can be wired to a UI button to leave the game and take the penalty.</summary>
    public void GiveUp()
    {
        if (IsRunning)
            FinishGame(false);
    }

    /// <summary>Called by the result panel button.</summary>
    public void CloseResult()
    {
        if (!resultShown || pendingResult == null)
            return;

        completionSource?.TrySetResult(pendingResult);
    }

    private void OnMiniGameFinished(bool success)
    {
        FinishGame(success);
    }

    private void FinishGame(bool success)
    {
        if (!IsRunning)
            return;

        activeEntry.Controller.Finished -= OnMiniGameFinished;
        activeEntry.Panel.SetActive(false);

        awardedCargo.Clear();
        Array.Clear(awardedCargoByCategory, 0, awardedCargoByCategory.Length);
        int awardedCoins = 0;
        int appliedPenalty = 0;

        if (success)
        {
            awardedCoins = coinReward;
            GameData.Instance.Wallet.AddGold(coinReward);
            GrantCargoRewards();
        }
        else if (player.ScoreSystem != null)
        {
            appliedPenalty = scorePenalty;
            player.ScoreSystem.MinusScore(scorePenalty);
        }

        pendingResult = new MiniGameResult(
            activeEntry.Id,
            success,
            awardedCoins,
            appliedPenalty,
            awardedCargo.ToArray());

        resultShown = true;
        ShowResult(success);
    }

    private void GrantCargoRewards()
    {
        if (rewardCatalog == null)
        {
            Debug.LogError("Mini-game reward catalog was not found.", this);
            return;
        }

        PlayerCargoModule cargoModule = player.CargoModule;
        TryGrantCargo(cargoModule, rewardCatalog.GetRandomRegular(), 0);
        TryGrantCargo(cargoModule, rewardCatalog.GetRandomTimed(), 1);
        TryGrantCargo(cargoModule, rewardCatalog.GetRandomHealth(), 2);
    }

    private void TryGrantCargo(
        PlayerCargoModule cargoModule,
        Cargo cargo,
        int categoryIndex)
    {
        if (cargo == null)
        {
            Debug.LogWarning("A mini-game cargo reward category is empty.", this);
            return;
        }

        if (cargoModule != null && cargoModule.TryTakeCargo(cargo))
        {
            awardedCargo.Add(cargo);
            awardedCargoByCategory[categoryIndex] = cargo;
            return;
        }

        Debug.LogWarning($"Could not add reward cargo '{cargo.name}' to the baggage.", this);
    }

    private void ShowResult(bool success)
    {
        if (success)
        {
            rewardPanelView.ShowSuccess(coinReward, awardedCargoByCategory);
        }
        else
        {
            rewardPanelView.ShowFailure(scorePenalty);
        }
    }

    private void DiscoverControllers()
    {
        entries.Clear();
        MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (!(behaviour is IMiniGameController controller))
                continue;

            if (!TryGetId(behaviour, out MiniGameId id))
                continue;

            entries.Add(new GameEntry
            {
                Id = id,
                Panel = behaviour.gameObject,
                Controller = controller
            });
        }

        if (entries.Count != 5)
            Debug.LogError($"Expected 5 mini-games, but found {entries.Count}.", this);
    }

    private static bool TryGetId(MonoBehaviour behaviour, out MiniGameId id)
    {
        if (behaviour is TowerBalanceMiniGame)
            id = MiniGameId.TowerBalance;
        else if (behaviour is TapAnimationStepper)
            id = MiniGameId.BaggagePacking;
        else if (behaviour is LabelMiniGame)
            id = MiniGameId.LabelPlacement;
        else if (behaviour is PackageSizeMiniGame)
            id = MiniGameId.PackageSize;
        else if (behaviour is BoxReturnMiniGame)
            id = MiniGameId.BoxReturn;
        else
        {
            id = default;
            return false;
        }

        return true;
    }

    private void SetAllGamePanelsActive(bool value)
    {
        for (int i = 0; i < entries.Count; i++)
            entries[i].Panel.SetActive(value);
    }

    private void OnDestroy()
    {
        if (activeEntry != null)
            activeEntry.Controller.Finished -= OnMiniGameFinished;

        if (rewardPanelView != null)
            rewardPanelView.ContinueRequested -= CloseResult;

        if (completionSource != null && !completionSource.Task.IsCompleted)
        {
            completionSource.TrySetException(
                new InvalidOperationException("Mini-game scene was closed before a result was accepted."));
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        minTimeLimitSeconds = Mathf.Max(0.1f, minTimeLimitSeconds);
        maxTimeLimitSeconds = Mathf.Max(minTimeLimitSeconds, maxTimeLimitSeconds);
    }
#endif
}
