using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Boosts/SpeedBoost Boost")]
public class SpeedBoostData : BoostData
{
    public float duration;

    public override IBoost Create(Player player)
    {
        return new SpeedBoost(player, this);
    }
}
