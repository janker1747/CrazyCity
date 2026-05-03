using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Boosts/Safe Boost")]
public class SafeBoostData : BoostData
{
    public override IBoost Create(Player player)
    {
        return new SafeBoost(player);
    }
}
