using UnityEngine;

[CreateAssetMenu(fileName = "SpeedBonusCargo", menuName = "Cargo/Speed Bonus Cargo")]
public class SpeedBonusCargo : Cargo
{
    [Header("Speed Bonus")]
    [SerializeField, Range(0f, 1f)] private float minValueMultiplier = 0.5f;

    public override int CalculateValue(float elapsedTime, float damageMultiplier)
    {
        if (DeliveryTime <= 0f)
            return Mathf.RoundToInt(MaxValue * Mathf.Clamp01(damageMultiplier));

        float remainingTimeRatio = 1f - Mathf.Clamp01(elapsedTime / DeliveryTime);
        float speedMultiplier = Mathf.Lerp(minValueMultiplier, 1f, remainingTimeRatio);

        return Mathf.RoundToInt(MaxValue * speedMultiplier * Mathf.Clamp01(damageMultiplier));
    }
}
