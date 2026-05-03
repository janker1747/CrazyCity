using System;
using UnityEngine;
public class JumpBoost : IBoost
{
    private Player _player;
    private JumpBoostData _data;

    public JumpBoost(Player player, JumpBoostData data)
    {
        _player = player;
        _data = data;
    }

    public void Activate()
    {
        _player.Rigidbody.AddForce(_player.transform.up * _data.force, ForceMode.Impulse);
    }
}