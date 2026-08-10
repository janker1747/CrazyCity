using System;
using UnityEngine;

public abstract class Cargo : ScriptableObject
{
    [Header("General")]
    [SerializeField] private string cargoName;
    [SerializeField] private Sprite icon;
    [SerializeField, Min(0)] private int maxValue = 100;

    [SerializeField] private bool hasDeliveryTimer = true;

    [SerializeField, Min(0f)] private float deliveryTime;

    [SerializeField, Min(1)] private int comboAmount = 1;
    [SerializeField] private bool hasHealth = true;
    [SerializeField] private int healthAmount = 0;

    [Header("Baggage Order")]
    [SerializeField] private int baggageOrder;
    
    [Header("Pickup Feedback")]
    [SerializeField] private ParticleSystem pickupParticlePrefab;

    public event Action<float> HealthGargoDelivered;
    public string CargoName => cargoName;
    public Sprite Icon => icon;
    public int MaxValue => maxValue;

    public bool HasHealth => hasHealth && healthAmount > 0;
    public int HealthAmount => HasHealth ? healthAmount : 0;
    public int BaggageOrder => baggageOrder;

    public bool HasDeliveryTimer => hasDeliveryTimer;

    public float DeliveryTime => hasDeliveryTimer ? deliveryTime : 0f;

    public virtual int ComboAmount => Mathf.Max(1, comboAmount);

    public ParticleSystem PickupParticlePrefab => pickupParticlePrefab;

    public void PlayPickupFeedback(Vector3 position)
    {
        GameAudio.PlaySfx(GameAudioCue.PickupCargo, position);

        if (pickupParticlePrefab == null)
            return;

        ImpactSystem impactSystem = ImpactSystem.Current;
        if (impactSystem == null)
            impactSystem = FindObjectOfType<ImpactSystem>();

        if (impactSystem == null)
        {
            Debug.LogWarning($"{nameof(Cargo)} {name}: cannot play pickup feedback because ImpactSystem was not found.");
            return;
        }

        impactSystem.PlayFeedback(position, pickupParticlePrefab);
    }

    public virtual void OnPickup(Player player) { }

    public virtual void OnPickup(Player player, PlayerCargoModule cargoModule, ActiveCargo activeCargo)
    {
        OnPickup(player);
    }

    public virtual void OnDeliver(Player player)
    {
        if (HasHealth)
        {
            HealthGargoDelivered?.Invoke(healthAmount);
        }
    }

    public virtual void OnFail(Player player) { }

    public virtual void OnTick(Player player, PlayerCargoModule cargoModule, ActiveCargo activeCargo, float deltaTime) { }

    public virtual void OnPlayerCollision(Player player, PlayerCargoModule cargoModule, ActiveCargo activeCargo, Collision collision) { }

    public virtual void OnPlayerScoreDamage(Player player, PlayerCargoModule cargoModule, ActiveCargo activeCargo, int damage) { }

    public virtual float GetTimerScaleForOtherCargo(Player player, ActiveCargo self, ActiveCargo otherCargo)
    {
        return 1f;
    }

    public virtual float GetGlobalRewardMultiplier(Player player, PlayerCargoModule cargoModule, ActiveCargo activeCargo)
    {
        return 1f;
    }

    public virtual float GetRewardMultiplierForCargo(
        Player player,
        PlayerCargoModule cargoModule,
        ActiveCargo self,
        ActiveCargo targetCargo)
    {
        return 1f;
    }

    public virtual int ModifyScoreDamage(Player player, PlayerCargoModule cargoModule, ActiveCargo activeCargo, int damage)
    {
        return damage;
    }

    public virtual bool ProvidesCargoProtection(Player player, PlayerCargoModule cargoModule, ActiveCargo activeCargo)
    {
        return false;
    }

    public virtual int CalculateValue(float elapsedTime, float damageMultiplier)
    {
        float safeMultiplier = Mathf.Clamp01(damageMultiplier);
        return Mathf.RoundToInt(maxValue * safeMultiplier);
    }

    public virtual int CalculateValue(Player player, PlayerCargoModule cargoModule, ActiveCargo activeCargo)
    {
        if (activeCargo == null)
            return 0;

        return CalculateValue(activeCargo.ElapsedTime, activeCargo.DamageMultiplier);
    }
}
