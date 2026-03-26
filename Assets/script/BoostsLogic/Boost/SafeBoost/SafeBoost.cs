public class SafeBoost : IBoost
{
    private Player _player;

    public SafeBoost(Player player)
    {
        _player = player;
    }

    public void Activate()
    {
        _player.ScoreSystem.ActivateSafe();
    }
}