using UnityEngine;

[CreateAssetMenu(menuName = "Boosts/Jump Boost")]
public class JumpBoostData : BoostData
{
    public float force;

    public override IBoost Create(Player player)
    {
        return new JumpBoost(player, this);
    }
}