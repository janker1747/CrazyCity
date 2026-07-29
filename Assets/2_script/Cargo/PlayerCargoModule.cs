using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCargoModule : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CargoArrowUI cargoArrowUI;
    [SerializeField] private CargoUIController cargoUIController;

    [Header("Fail Sequence")]
    [SerializeField] private float failInterval = 0.08f;
    [SerializeField] private AudioClip failSound;
    [SerializeField] private ParticleSystem failParticle;

    [Header("Success Sequence")]
    [Header("Success Sequence")]
    [SerializeField] private float successInterval = 0.05f;
    [SerializeField] private AudioClip successSound;
    [SerializeField] private ParticleSystem successParticle;

    [Header("Combo Reward")]
    [SerializeField] private int comboBonusPerCargo = 25;

    [SerializeField]
    private AnimationCurve comboBonusCurve =
        AnimationCurve.EaseInOut(1f, 1f, 10f, 3f);

    private readonly CargoBaggage baggage = new();

    private Player player;
    private CargoManager cargoManager;
    private DeliveryPoint currentDeliveryPoint;
    private bool InHaveHealCargo;
    
    private bool isFailSequenceRunning;
    private bool isSuccessSequenceRunning;

    public event Action<ActiveCargo> CargoAdded;
    public event Action<ActiveCargo> CargoRemoved;
    public event Action CargoOrderChanged;
    public event Action<int> ComboChanged;

    public IReadOnlyList<ActiveCargo> ActiveCargos => baggage.ActiveCargos;

    public Cargo CurrentCargo => baggage.CurrentCargo != null
        ? baggage.CurrentCargo.Cargo
        : null;

    public bool HasActiveCargo => baggage.Count > 0;
    public int ActiveCargoCount => baggage.Count;
    public int CurrentComboAmount => baggage.CurrentComboAmount;

    public bool CanTakeCargo =>
        !isFailSequenceRunning &&
        !isSuccessSequenceRunning;

    public void Initialize(
        Player owner,
        CargoManager manager,
        CargoArrowUI arrowUI,
        CargoUIController uiController = null)
    {
        player = owner;
        cargoManager = manager;
        cargoArrowUI = arrowUI;

        if (uiController != null)
            cargoUIController = uiController;

        if (cargoArrowUI != null && player != null)
            cargoArrowUI.SetPlayer(player.transform);

        UnsubscribeBaggageEvents();
        baggage.Initialize(cargoUIController);
        SubscribeBaggageEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeBaggageEvents();
        baggage.Dispose();
    }

    public void Tick(float deltaTime)
    {
        if (baggage.Count == 0)
            return;

        if (isFailSequenceRunning || isSuccessSequenceRunning)
            return;

        float timerScale = CalculateGlobalTimerScale();
        baggage.Tick(deltaTime, timerScale);

        if (isFailSequenceRunning || isSuccessSequenceRunning)
            return;

        IReadOnlyList<ActiveCargo> activeCargos = baggage.ActiveCargos;

        for (int i = activeCargos.Count - 1; i >= 0; i--)
        {
            if (i >= activeCargos.Count)
                continue;

            ActiveCargo activeCargo = activeCargos[i];

            if (activeCargo == null || activeCargo.Cargo == null)
            {
                baggage.RemoveCargoAt(i, out _);
                continue;
            }

            activeCargo.Cargo.OnTick(
                player,
                this,
                activeCargo,
                deltaTime);
        }

        RefreshDeliveryState();
    }

    public bool TryTakeCargo(Cargo cargo)
    {
        if (!CanTakeCargo)
            return false;

        if (player == null)
            player = GetComponent<Player>();

        if (player == null)
        {
            Debug.LogWarning(
                $"{nameof(PlayerCargoModule)} on {name}: Player component is missing.");
            return false;
        }

        if (cargo == null)
        {
            Debug.LogWarning(
                $"{nameof(PlayerCargoModule)} on {name}: cargo is null.");
            return false;
        }

        if (cargoManager == null)
        {
            Debug.LogWarning(
                $"{nameof(PlayerCargoModule)} on {name}: CargoManager is not assigned.");
            return false;
        }

        if (currentDeliveryPoint == null)
        {
            currentDeliveryPoint =
                cargoManager.CreateDeliveryPointForPlayer(
                    player.transform.position,
                    player);

            if (currentDeliveryPoint == null)
            {
                Debug.LogWarning(
                    $"{nameof(PlayerCargoModule)} on {name}: failed to create delivery point.");
                return false;
            }
        }

        ActiveCargo activeCargo = baggage.AddCargo(cargo);

        if (activeCargo == null)
            return false;

        cargo.OnPickup(player, this, activeCargo);
        cargo.PlayPickupFeedback(player.transform.position);

        return true;
    }

    public void CompleteDelivery(bool success)
    {
        if (baggage.Count == 0)
        {
            Debug.LogWarning(
                $"{nameof(PlayerCargoModule)} on {name}: no cargo to complete.");
            return;
        }

        if (success)
            StartSuccessSequence();
        else
            StartFailSequence();
    }

    public bool CompleteDelivery(Cargo cargo, bool success)
    {
        int cargoIndex = baggage.FindCargoIndex(cargo);

        if (cargoIndex < 0)
            return false;

        CompleteCargoAt(cargoIndex, success, false);

        RefreshDeliveryState();

        return true;
    }

    public void FailDelivery()
    {
        if (baggage.Count == 0)
            return;

        StartFailSequence();
    }

    public void FailAllDeliveries()
    {
        FailDelivery();
    }

    public void NotifyPlayerCollision(Collision collision)
    {
        if (isFailSequenceRunning || isSuccessSequenceRunning)
            return;

        IReadOnlyList<ActiveCargo> activeCargos = baggage.ActiveCargos;

        for (int i = activeCargos.Count - 1; i >= 0; i--)
        {
            if (i >= activeCargos.Count)
                continue;

            ActiveCargo activeCargo = activeCargos[i];

            if (activeCargo == null || activeCargo.Cargo == null)
                continue;

            activeCargo.Cargo.OnPlayerCollision(
                player,
                this,
                activeCargo,
                collision);
        }

        RefreshDeliveryState();
    }

    public int ModifyScoreDamage(int damage)
    {
        if (isFailSequenceRunning || isSuccessSequenceRunning)
            return damage;

        int modifiedDamage = Mathf.Max(0, damage);
        IReadOnlyList<ActiveCargo> activeCargos = baggage.ActiveCargos;

        for (int i = 0; i < activeCargos.Count; i++)
        {
            ActiveCargo activeCargo = activeCargos[i];

            if (activeCargo == null || activeCargo.Cargo == null)
                continue;

            modifiedDamage = Mathf.Max(
                0,
                activeCargo.Cargo.ModifyScoreDamage(
                    player,
                    this,
                    activeCargo,
                    modifiedDamage));
        }

        return modifiedDamage;
    }

    public void NotifyPlayerScoreDamage(int damage)
    {
        if (isFailSequenceRunning || isSuccessSequenceRunning)
            return;

        IReadOnlyList<ActiveCargo> activeCargos = baggage.ActiveCargos;

        for (int i = activeCargos.Count - 1; i >= 0; i--)
        {
            if (i >= activeCargos.Count)
                continue;

            ActiveCargo activeCargo = activeCargos[i];

            if (activeCargo == null || activeCargo.Cargo == null)
                continue;

            activeCargo.Cargo.OnPlayerScoreDamage(
                player,
                this,
                activeCargo,
                damage);
        }

        RefreshDeliveryState();
    }

    public bool HasCargoProtection(ActiveCargo ignoredCargo = null)
    {
        if (player != null && player.HasShield)
            return true;

        IReadOnlyList<ActiveCargo> activeCargos = baggage.ActiveCargos;

        for (int i = 0; i < activeCargos.Count; i++)
        {
            ActiveCargo activeCargo = activeCargos[i];

            if (activeCargo == null ||
                activeCargo == ignoredCargo ||
                activeCargo.Cargo == null)
                continue;

            if (activeCargo.Cargo.ProvidesCargoProtection(
                    player,
                    this,
                    activeCargo))
                return true;
        }

        return false;
    }

    public int CountCargo(Cargo cargo)
    {
        return baggage.CountCargo(cargo);
    }

    public int GetCargoIndex(ActiveCargo activeCargo)
    {
        return baggage.IndexOf(activeCargo);
    }

    public bool AreNeighbors(ActiveCargo firstCargo, ActiveCargo secondCargo)
    {
        return baggage.AreNeighbors(firstCargo, secondCargo);
    }

    public void SetDamageMultiplier(float multiplier)
    {
        float safeMultiplier = Mathf.Clamp01(multiplier);
        IReadOnlyList<ActiveCargo> activeCargos = baggage.ActiveCargos;

        for (int i = 0; i < activeCargos.Count; i++)
            activeCargos[i].DamageMultiplier = safeMultiplier;
    }

    public bool SetDamageMultiplier(Cargo cargo, float multiplier)
    {
        int cargoIndex = baggage.FindCargoIndex(cargo);

        if (cargoIndex < 0)
            return false;

        ActiveCargo activeCargo = baggage.GetCargoAt(cargoIndex);
        if (activeCargo == null)
            return false;

        activeCargo.DamageMultiplier = Mathf.Clamp01(multiplier);

        return true;
    }

    public bool TryFailCargo(ActiveCargo activeCargo)
    {
        int index = baggage.IndexOf(activeCargo);

        if (index < 0)
            return false;

        CompleteCargoAt(index, false, false);

        RefreshDeliveryState();

        return true;
    }

    public bool TakeDamage(int damage)
    {
        InHaveHealCargo = baggage.TakeHealthDamage(damage);
    
        return InHaveHealCargo;
    }

    private void StartFailSequence()
    {
        StartFailSequence(null);
    }

    private void StartFailSequence(IReadOnlyList<ActiveCargo> cargos)
    {
        StartCoroutine(FailSequenceCoroutine(cargos));
    }

    private void StartSuccessSequence()
    {
        if (isSuccessSequenceRunning)
            return;

        isSuccessSequenceRunning = true;
        StartCoroutine(SuccessSequenceCoroutine());
    }

    private IEnumerator FailSequenceCoroutine(IReadOnlyList<ActiveCargo> cargos)
    {
        isFailSequenceRunning = true;

        float currentDelay = failInterval;

        if (cargos == null)
        {
            while (baggage.Count > 0)
            {
                int lastIndex = baggage.Count - 1;
                ActiveCargo activeCargo = baggage.GetCargoAt(lastIndex);

                if (activeCargo != null && activeCargo.Cargo != null)
                    PlayCargoFailFeedback();

                CompleteCargoAt(lastIndex, false, false);

                yield return new WaitForSeconds(currentDelay);

                currentDelay *= 0.9f;
            }
        }
        else
        {
            List<ActiveCargo> expiredCargos = new(cargos);

            for (int i = expiredCargos.Count - 1; i >= 0; i--)
            {
                ActiveCargo activeCargo = expiredCargos[i];
                int cargoIndex = baggage.IndexOf(activeCargo);

                if (cargoIndex < 0)
                    continue;

                if (activeCargo != null && activeCargo.Cargo != null)
                    PlayCargoFailFeedback();

                CompleteCargoAt(cargoIndex, false, false);

                yield return new WaitForSeconds(currentDelay);

                currentDelay *= 0.9f;
            }
        }

        RefreshDeliveryState();

        isFailSequenceRunning = false;
    }

    private IEnumerator SuccessSequenceCoroutine()
    {
        int deliveredCargoCount = baggage.Count;

        float rewardMultiplier = CalculateGlobalRewardMultiplier();

        float currentDelay = successInterval;

        while (baggage.Count > 0)
        {
            int lastIndex = baggage.Count - 1;
            ActiveCargo activeCargo = baggage.GetCargoAt(lastIndex);

            if (activeCargo != null &&
                activeCargo.Cargo != null)
            {
                PlayCargoSuccessFeedback();

                DeliverCargo(activeCargo.Cargo);
                GameData.Instance.AddCargo(activeCargo.Cargo);

                int reward =
                    CalculateReward(
                        activeCargo,
                        rewardMultiplier);

                if (reward > 0 &&
                    player != null &&
                    player.ScoreSystem != null)
                {
                    player.ScoreSystem.AddScore(reward);
                }
            }

            baggage.RemoveCargoAt(lastIndex, out _);

            yield return new WaitForSeconds(currentDelay);

            currentDelay *= 0.92f;
        }

        GiveComboBonus(deliveredCargoCount);

        cargoManager?.NotifySuccessfulDelivery();
        RefreshDeliveryState();

        isSuccessSequenceRunning = false;
    }

    private void GiveComboBonus(int cargoCount)
    {
        if (cargoCount <= 1)
            return;

        float comboMultiplier =
            comboBonusCurve.Evaluate(cargoCount);

        int comboReward =
            Mathf.RoundToInt(
                cargoCount *
                comboBonusPerCargo *
                comboMultiplier);

        if (comboReward <= 0)
            return;

        if (player != null &&
            player.ScoreSystem != null)
        {
            player.ScoreSystem.AddScore(comboReward);
        }

        Debug.Log($"COMBO BONUS +{comboReward}");
    }

    private void PlayCargoFailFeedback()
    {
        Vector3 position =
            player != null
                ? player.transform.position
                : transform.position;

        if (failParticle != null)
            Instantiate(failParticle, position, Quaternion.identity);

        if (failSound != null)
            AudioSource.PlayClipAtPoint(failSound, position);
    }

    private void PlayCargoSuccessFeedback()
    {
        Vector3 position =
            player != null
                ? player.transform.position
                : transform.position;

        if (successParticle != null)
            Instantiate(successParticle, position, Quaternion.identity);

        if (successSound != null)
            AudioSource.PlayClipAtPoint(successSound, position);
    }

    private void CompleteCargoAt(
        int cargoIndex,
        bool success,
        bool useComboReward)
    {
        if (!baggage.RemoveCargoAt(cargoIndex, out ActiveCargo activeCargo))
            return;

        if (activeCargo == null || activeCargo.Cargo == null)
            return;

        if (success)
        {
            DeliverCargo(activeCargo.Cargo);
            GameData.Instance.AddCargo(activeCargo.Cargo);

            int reward = CalculateReward(activeCargo, 1f);

            if (useComboReward)
                reward *= Mathf.Max(1, activeCargo.ComboAmount);

            if (reward > 0 &&
                player != null &&
                player.ScoreSystem != null)
            {
                player.ScoreSystem.AddScore(reward);
            }
        }
        else
        {
            activeCargo.Cargo.OnFail(player);
        }
    }

    private void DeliverCargo(Cargo cargo)
    {
        if (cargo == null)
            return;

        PlayerHealth playerHealth = player != null ? player.Health : null;

        if (playerHealth != null)
            cargo.HealthGargoDelivered += playerHealth.AddHealth;

        try
        {
            cargo.OnDeliver(player);
        }
        finally
        {
            if (playerHealth != null)
                cargo.HealthGargoDelivered -= playerHealth.AddHealth;
        }
    }

    private float CalculateGlobalTimerScale()
    {
        float timerScale = 1f;
        IReadOnlyList<ActiveCargo> activeCargos = baggage.ActiveCargos;

        for (int i = 0; i < activeCargos.Count; i++)
        {
            ActiveCargo activeCargo = activeCargos[i];

            if (activeCargo == null || activeCargo.Cargo == null)
                continue;

            timerScale *= Mathf.Max(
                0f,
                activeCargo.Cargo.GetTimerScaleForOtherCargo(
                    player,
                    activeCargo,
                    activeCargo));
        }

        return timerScale;
    }

    private float CalculateGlobalRewardMultiplier()
    {
        float multiplier = 1f;
        IReadOnlyList<ActiveCargo> activeCargos = baggage.ActiveCargos;

        for (int i = 0; i < activeCargos.Count; i++)
        {
            ActiveCargo activeCargo = activeCargos[i];

            if (activeCargo == null || activeCargo.Cargo == null)
                continue;

            multiplier *= Mathf.Max(
                0f,
                activeCargo.Cargo.GetGlobalRewardMultiplier(
                    player,
                    this,
                    activeCargo));
        }

        return multiplier;
    }

    private int CalculateReward(
        ActiveCargo targetCargo,
        float globalMultiplier)
    {
        if (targetCargo == null || targetCargo.Cargo == null)
            return 0;

        int baseReward = Mathf.Max(
            0,
            targetCargo.Cargo.CalculateValue(
                player,
                this,
                targetCargo));

        float cargoMultiplier =
            CalculateRewardMultiplierForCargo(targetCargo);

        return Mathf.RoundToInt(
            baseReward *
            Mathf.Max(0f, globalMultiplier) *
            cargoMultiplier);
    }

    private float CalculateRewardMultiplierForCargo(
        ActiveCargo targetCargo)
    {
        float multiplier = 1f;
        IReadOnlyList<ActiveCargo> activeCargos = baggage.ActiveCargos;

        for (int i = 0; i < activeCargos.Count; i++)
        {
            ActiveCargo sourceCargo = activeCargos[i];

            if (sourceCargo == null ||
                sourceCargo == targetCargo ||
                sourceCargo.Cargo == null)
                continue;

            multiplier *= Mathf.Max(
                0f,
                sourceCargo.Cargo.GetRewardMultiplierForCargo(
                    player,
                    this,
                    sourceCargo,
                    targetCargo));
        }

        return multiplier;
    }

    private void RefreshDeliveryState()
    {
        if (baggage.Count > 0)
            return;

        cargoUIController?.ResetTimer();

        currentDeliveryPoint = null;

        if (cargoManager != null)
            cargoManager.OnDeliveryFinished();
    }

    private void SubscribeBaggageEvents()
    {
        baggage.CargoAdded += OnCargoAdded;
        baggage.CargoRemoved += OnCargoRemoved;
        baggage.CargoOrderChanged += OnCargoOrderChanged;
        baggage.ComboChanged += OnComboChanged;
        baggage.TimedCargosExpired += OnTimedCargosExpired;
        baggage.HealthCargosExpired += OnHealthCargosExpired;
    }

    private void UnsubscribeBaggageEvents()
    {
        baggage.CargoAdded -= OnCargoAdded;
        baggage.CargoRemoved -= OnCargoRemoved;
        baggage.CargoOrderChanged -= OnCargoOrderChanged;
        baggage.ComboChanged -= OnComboChanged;
        baggage.TimedCargosExpired -= OnTimedCargosExpired;
        baggage.HealthCargosExpired -= OnHealthCargosExpired;
    }

    private void OnCargoAdded(ActiveCargo cargo)
    {
        CargoAdded?.Invoke(cargo);
    }

    private void OnCargoRemoved(ActiveCargo cargo)
    {
        CargoRemoved?.Invoke(cargo);
    }

    private void OnCargoOrderChanged()
    {
        CargoOrderChanged?.Invoke();
    }

    private void OnComboChanged(int comboAmount)
    {
        ComboChanged?.Invoke(comboAmount);
    }

    private void OnTimedCargosExpired(IReadOnlyList<ActiveCargo> cargos)
    {
        StartFailSequence(cargos);
    }

    private void OnHealthCargosExpired(IReadOnlyList<ActiveCargo> cargos)
    {
        StartFailSequence(cargos);
    }
}
