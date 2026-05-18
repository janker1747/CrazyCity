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
    [SerializeField] private float successInterval = 0.05f;
    [SerializeField] private AudioClip successSound;
    [SerializeField] private ParticleSystem successParticle;

    [Header("Combo Reward")]
    [SerializeField] private int comboBonusPerCargo = 25;

    [SerializeField]
    private AnimationCurve comboBonusCurve =
        AnimationCurve.EaseInOut(1f, 1f, 10f, 3f);

    private Player player;
    private CargoManager cargoManager;
    private DeliveryPoint currentDeliveryPoint;
    private Health cargoHealth;
    
    private readonly List<ActiveCargo> activeCargos = new();

    private bool isFailSequenceRunning;
    private bool isSuccessSequenceRunning;
    private bool isHealthActivate;

    private int currentComboAmount;

    public event Action<ActiveCargo> CargoAdded;
    public event Action<ActiveCargo> CargoRemoved;
    public event Action<int> ComboChanged;

    public IReadOnlyList<ActiveCargo> ActiveCargos => activeCargos;

    public Cargo CurrentCargo =>
        activeCargos.Count > 0
            ? activeCargos[0].Cargo
            : null;

    public bool HasActiveCargo => activeCargos.Count > 0;

    public int ActiveCargoCount => activeCargos.Count;

    public int CurrentComboAmount => currentComboAmount;

    public bool CanTakeCargo =>
        !isFailSequenceRunning &&
        !isSuccessSequenceRunning;

    public void Initialize(
        Player owner,
        CargoManager manager,
        CargoArrowUI arrowUI)
    {
        player = owner;
        cargoManager = manager;
        cargoArrowUI = arrowUI;
        cargoHealth = new Health();

        if (cargoArrowUI != null && player != null)
            cargoArrowUI.SetPlayer(player.transform);

        if (cargoUIController != null)
        {
            cargoUIController.TimerCompleted -= OnTimerCompleted;
            cargoUIController.TimerCompleted += OnTimerCompleted;
        }
    }

    private void OnDestroy()
    {
        if (cargoUIController != null)
            cargoUIController.TimerCompleted -= OnTimerCompleted;
    }

    public void Tick(float deltaTime)
    {
        if (activeCargos.Count == 0)
            return;

        if (isFailSequenceRunning || isSuccessSequenceRunning)
            return;

        float timerScale = CalculateGlobalTimerScale();

        cargoUIController?.Tick(deltaTime, timerScale);

        for (int i = activeCargos.Count - 1; i >= 0; i--)
        {
            if (i >= activeCargos.Count)
                continue;

            ActiveCargo activeCargo = activeCargos[i];

            if (activeCargo == null || activeCargo.Cargo == null)
            {
                RemoveCargoAt(i, null);
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

        ActiveCargo activeCargo = new ActiveCargo(cargo);

        activeCargos.Add(activeCargo);

        if (cargo.HasDeliveryTimer)
            cargoUIController?.AddCargoTime(cargo.DeliveryTime);

        cargo.OnPickup(player, this, activeCargo);

        cargo.PlayPickupFeedback(player.transform.position);

        CargoAdded?.Invoke(activeCargo);

        NotifyComboChanged();

        if (isHealthActivate == false)
        {
            cargoHealth.Initialize(cargo.HealthAmount);
        }
        else
        {
            cargoHealth.AddHealth(cargo.HealthAmount);
        }
        
        return true;
    }

    public void CompleteDelivery(bool success)
    {
        if (activeCargos.Count == 0)
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
        int cargoIndex = FindCargoIndex(cargo);

        if (cargoIndex < 0)
            return false;

        CompleteCargoAt(cargoIndex, success, false);

        RefreshDeliveryState();

        return true;
    }

    public void FailDelivery()
    {
        if (activeCargos.Count == 0)
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
        if (cargo == null)
            return 0;

        int count = 0;

        for (int i = 0; i < activeCargos.Count; i++)
        {
            if (activeCargos[i] != null &&
                activeCargos[i].Cargo == cargo)
            {
                count++;
            }
        }

        return count;
    }

    public void SetDamageMultiplier(float multiplier)
    {
        float safeMultiplier = Mathf.Clamp01(multiplier);

        for (int i = 0; i < activeCargos.Count; i++)
            activeCargos[i].DamageMultiplier = safeMultiplier;
    }

    public bool SetDamageMultiplier(Cargo cargo, float multiplier)
    {
        int cargoIndex = FindCargoIndex(cargo);

        if (cargoIndex < 0)
            return false;

        activeCargos[cargoIndex].DamageMultiplier =
            Mathf.Clamp01(multiplier);

        return true;
    }

    public bool TryFailCargo(ActiveCargo activeCargo)
    {
        int index = activeCargos.IndexOf(activeCargo);

        if (index < 0)
            return false;

        CompleteCargoAt(index, false, false);

        RefreshDeliveryState();

        return true;
    }

    private void OnTimerCompleted()
    {
        StartFailSequence();
    }

    private void StartFailSequence()
    {
        if (isFailSequenceRunning)
            return;

        StartCoroutine(FailSequenceCoroutine());
    }

    private void StartSuccessSequence()
    {
        if (isSuccessSequenceRunning)
            return;

        StartCoroutine(SuccessSequenceCoroutine());
    }

    private IEnumerator FailSequenceCoroutine()
    {
        isFailSequenceRunning = true;

        float currentDelay = failInterval;

        while (activeCargos.Count > 0)
        {
            int lastIndex = activeCargos.Count - 1;

            ActiveCargo activeCargo = activeCargos[lastIndex];

            if (activeCargo != null && activeCargo.Cargo != null)
            {
                PlayCargoFailFeedback();

                CompleteCargoAt(lastIndex, false, false);
            }

            yield return new WaitForSeconds(currentDelay);

            currentDelay *= 0.9f;
        }

        RefreshDeliveryState();

        isFailSequenceRunning = false;
    }

    private IEnumerator SuccessSequenceCoroutine()
    {
        isSuccessSequenceRunning = true;

        int deliveredCargoCount = activeCargos.Count;

        float rewardMultiplier = CalculateGlobalRewardMultiplier();

        float currentDelay = successInterval;

        while (activeCargos.Count > 0)
        {
            int lastIndex = activeCargos.Count - 1;

            ActiveCargo activeCargo = activeCargos[lastIndex];

            if (activeCargo != null &&
                activeCargo.Cargo != null)
            {
                PlayCargoSuccessFeedback();

                activeCargo.Cargo.OnDeliver(player);

                int reward =
                    Mathf.RoundToInt(
                        activeCargo.Cargo.CalculateValue(
                            player,
                            this,
                            activeCargo)
                        * rewardMultiplier);

                if (reward > 0 &&
                    player != null &&
                    player.ScoreSystem != null)
                {
                    player.ScoreSystem.AddScore(reward);
                }

                RemoveCargoAt(lastIndex, activeCargo);
            }

            yield return new WaitForSeconds(currentDelay);

            currentDelay *= 0.92f;
        }

        GiveComboBonus(deliveredCargoCount);

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
        if (cargoIndex < 0 || cargoIndex >= activeCargos.Count)
            return;

        ActiveCargo activeCargo = activeCargos[cargoIndex];

        RemoveCargoAt(cargoIndex, activeCargo);

        if (activeCargo == null || activeCargo.Cargo == null)
            return;

        if (success)
        {
            activeCargo.Cargo.OnDeliver(player);

            int reward = Mathf.Max(
                0,
                activeCargo.Cargo.CalculateValue(
                    player,
                    this,
                    activeCargo));

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

    private void RemoveCargoAt(
        int cargoIndex,
        ActiveCargo activeCargo)
    {
        if (cargoIndex < 0 || cargoIndex >= activeCargos.Count)
            return;

        if (activeCargo == null)
            activeCargo = activeCargos[cargoIndex];

        activeCargos.RemoveAt(cargoIndex);

        CargoRemoved?.Invoke(activeCargo);

        NotifyComboChanged();
    }

    private int FindCargoIndex(Cargo cargo)
    {
        if (cargo == null)
            return -1;

        for (int i = 0; i < activeCargos.Count; i++)
        {
            if (activeCargos[i] != null &&
                activeCargos[i].Cargo == cargo)
            {
                return i;
            }
        }

        return -1;
    }

    private float CalculateGlobalTimerScale()
    {
        float timerScale = 1f;

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

    private int CalculateCurrentComboAmount()
    {
        int comboAmount = 0;

        for (int i = 0; i < activeCargos.Count; i++)
        {
            if (activeCargos[i] != null)
                comboAmount += activeCargos[i].ComboAmount;
        }

        return comboAmount;
    }

    private void NotifyComboChanged()
    {
        currentComboAmount = CalculateCurrentComboAmount();

        ComboChanged?.Invoke(currentComboAmount);
    }

    private void RefreshDeliveryState()
    {
        if (activeCargos.Count > 0)
            return;

        cargoUIController?.ResetTimer();

        currentDeliveryPoint = null;

        if (cargoManager != null)
            cargoManager.OnDeliveryFinished();
    }

    public void TakeDamage(int damage) => cargoHealth.TakeDamage(damage);
}