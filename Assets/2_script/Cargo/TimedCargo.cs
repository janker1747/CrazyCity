using UnityEngine;

[CreateAssetMenu(fileName = "TimedCargo", menuName = "Cargo/Timed Cargo")]
public class TimedCargo : Cargo
{
    [Header("Timed Cargo")]
    [SerializeField, Min(1)] private int lives = 1;

    public int Lives => lives;

    public override int CalculateValue(float elapsedTime, float damageMultiplier)
    {
        if (DeliveryTime > 0f && elapsedTime > DeliveryTime)
            return 0;

        return Mathf.RoundToInt(MaxValue * Mathf.Clamp01(damageMultiplier));
    }
}
