using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedBoost : ITimedBoost
{
    private Player _player;
    private float _duration;
    private float _timer;
    private TimerUI _timerUI;
    private float _bonusSpeed = 40f;

    public float Duration => _duration;
    public bool IsFinished => _timer <= 0;

    public SpeedBoost(Player player, SpeedBoostData data)
    {
        _player = player;
        _duration = data.duration;
        _timerUI = _player.Timer;
    }

    public void Activate()
    {
        _timer = _duration;

        _player.AddSpeed(_bonusSpeed);
        _timerUI.StartTimer(_timer);
    }

    public void Tick(float deltaTime)
    {
        _timer -= deltaTime;
    }

    public void Deactivate()
    {
        _timerUI.StopTimer();
        _player.EndBonusSpeed();
    }
}
