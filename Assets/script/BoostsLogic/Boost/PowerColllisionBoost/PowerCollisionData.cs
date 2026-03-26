using UnityEngine;

[CreateAssetMenu(menuName = "Boosts/PowerCollision")]
public class PowerCollisionData : BoostData
{
    public override IBoost Create(Player player)
    {
        return new PowerCollision(player);
    }
}
