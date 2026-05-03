using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Boosts/TimeStop")]
public class TimeStopData : BoostData
{
    [Header("Particles")]
    public ParticleSystem particlePrefab;

    [Header("Sound")]
    public AudioClip sound;

    public float duration;

    public override IBoost Create(Player player)
    {
        return new TimeStopBoost(player, this);
    }
}
