using UnityEngine;

[CreateAssetMenu(menuName = "Boosts/Boost Data")]
public abstract class BoostData : ScriptableObject
{
    public abstract IBoost Create(Player player);
    public Sprite sprite;
}