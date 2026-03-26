using UnityEngine;

public class TimeStopBoost : ITimedBoost
{
    private float _duration;
    private float _timer;

    private TimerUI _timerUI;
    private TimeStopManager _manager;

    public float Duration => _duration;

    public TimeStopBoost(Player player, TimeStopData data)
    {
        _timerUI = player.Timer;
        _duration = data.duration;
        _manager = player.Stoper;
    }

    public void Activate()
    {
        _timer = _duration;

        _manager.Freeze();
        _timerUI.StartTimer(_timer);
    }

    public void Tick(float deltaTime)
    {
        _timer -= deltaTime;
    }

    public void Deactivate()
    {
        _manager.Unfreeze();
        _timerUI?.StopTimer();
    }
}