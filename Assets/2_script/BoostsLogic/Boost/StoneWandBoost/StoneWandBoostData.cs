using UnityEngine;

[CreateAssetMenu(menuName = "Boosts/Stone Wand")]
public class StoneWandBoostData : BoostData
{
    public GameObject rockPrefab;

    public override IBoost Create(Player player)
    {
        return new StoneWandBoost(player, this);
    }
}