public class DoublePointBoost : ITimedBoost
{
    private Player _player;
    private float _duration;
    private float _timer;
    private TimerUI _timerUI;

    public float Duration => _duration;
    public bool IsFinished => _timer <= 0;

    public DoublePointBoost(Player player, DoublePointData data)
    {
        _player = player;
        _duration = data.duration;
        _timerUI = _player.Timer;
    }

    public void Activate()
    {
        _timer = _duration;

        _player.ScoreSystem.SetMultiplier(2f);
        _timerUI.StartTimer(_timer);
    }

    public void Tick(float deltaTime)
    {
        _timer -= deltaTime;
    }

    public void Deactivate()
    {
        _timerUI.StopTimer();
        _player.ScoreSystem.SetMultiplier(1f);
    }
}