using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Boosts/DoublePoint Boost")]
public class DoublePointData : BoostData
{
    public float duration;

    public override IBoost Create(Player player)
    {
        return new DoublePointBoost(player, this);
    }
}
