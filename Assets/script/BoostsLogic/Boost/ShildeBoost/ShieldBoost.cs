using UnityEngine;

public class ShieldBoost : IBoost
{
    private Player _player;

    public ShieldBoost(Player player)
    {
        _player = player;
    }

    public void Activate()
    {
        _player.EnableShield();
    }
}