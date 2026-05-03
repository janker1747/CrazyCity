using UnityEngine;

[CreateAssetMenu(fileName = "RegularCargo", menuName = "Cargo/Regular Cargo")]
public class RegularCargo : Cargo
{
    [Header("Regular Cargo")]
    [SerializeField, Min(1)] private int maxLives = 3;
    [SerializeField, Range(0f, 1f)] private float valueLossPerLostLife = 0.33f;

    public int MaxLives => maxLives;

    public float CalculateDamageMultiplier(int livesLeft)
    {
        int safeLives = Mathf.Clamp(livesLeft, 0, maxLives);
        int lostLives = maxLives - safeLives;

        return Mathf.Clamp01(1f - lostLives * valueLossPerLostLife);
    }

    public override int CalculateValue(float elapsedTime, float damageMultiplier)
    {
        return Mathf.RoundToInt(MaxValue * Mathf.Clamp01(damageMultiplier));
    }
}
