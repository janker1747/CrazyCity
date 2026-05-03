using UnityEngine;

public abstract class Cargo : ScriptableObject
{
    [Header("General")]
    [SerializeField] private string cargoName;
    [SerializeField] private Sprite icon;
    [SerializeField, Min(0)] private int maxValue = 100;
    [SerializeField, Min(0f)] private float deliveryTime;

    public string CargoName => cargoName;
    public Sprite Icon => icon;
    public int MaxValue => maxValue;
    public float DeliveryTime => deliveryTime;

    public virtual void OnPickup(Player player) { }

    public virtual void OnDeliver(Player player) { }

    public virtual void OnFail(Player player) { }

    public virtual int CalculateValue(float elapsedTime, float damageMultiplier)
    {
        float safeMultiplier = Mathf.Clamp01(damageMultiplier);
        return Mathf.RoundToInt(maxValue * safeMultiplier);
    }
}
