public class PowerCollision : IEventBoost
{
    private PlayerCollisionHandler _collisionHandler;
    private UiPlayer _ui;

    private bool _isWaitingHit;

    public PowerCollision(Player player)
    {
        _ui = player.UI;
        _collisionHandler = player.PlayerCollision;
    }

    public void Activate()
    {
        _isWaitingHit = true;
    }

    public void Subscribe()
    {
        _collisionHandler.OnPoliceImpact += HandlePoliceImpact;
        _collisionHandler.PowerCollisionOn();
        _ui.EnableImage("PowerCollision");
    }

    public void Unsubscribe()
    {
        _collisionHandler.OnPoliceImpact -= HandlePoliceImpact;
        _collisionHandler.PowerCollisionOff();
        _ui.DisableImage("PowerCollision");
        _isWaitingHit = false;
    }

    private void HandlePoliceImpact()
    {
        if (!_isWaitingHit)
            return;

        _isWaitingHit = false;
        Unsubscribe();
    }
}
