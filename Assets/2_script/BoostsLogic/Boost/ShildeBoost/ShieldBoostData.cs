using UnityEngine;

[CreateAssetMenu(menuName = "Boosts/Shild")]
public class ShieldBoostData : BoostData
{
    public override IBoost Create(Player player)
    {
        return new ShieldBoost(player);
    }
}