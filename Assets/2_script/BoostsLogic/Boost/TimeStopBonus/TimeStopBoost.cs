using UnityEngine;

public class TimeStopBoost : ITimedBoost
{
    private float _duration;
    private float _timer;

    private TimeStopData _data;
    private TimerUI _timerUI;
    private TimeStopManager _manager;
    Player _player;

    public float Duration => _duration;
    public bool IsFinished => _timer <= 0f;


    public TimeStopBoost(Player player, TimeStopData data)
    {
        _timerUI = player.Timer;
        _duration = data.duration;
        _manager = player.Stoper;
        _player = player;
        _data = data;
    }

    public void Activate()
    {
        _timer = _duration;

        _manager.Freeze();
        _timerUI.StartTimer(_timer);
        PlayParticles();
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

    private void PlayParticles()
    {
        if (_data.particlePrefab == null)
            return;

        ParticleSystem particle = Object.Instantiate(_data.particlePrefab);
        particle.transform.position = _player.transform.position + Vector3.up * 1.5f;
        particle.Play();
    }
}