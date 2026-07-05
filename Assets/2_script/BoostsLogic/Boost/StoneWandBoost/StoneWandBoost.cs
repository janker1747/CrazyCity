using DG.Tweening;
using UnityEngine;

public class StoneWandBoost : IEventBoost
{
    private Player _player;
    private PlayerCollisionHandler _collisionHandler;
    private StoneWandBoostData _data;

    private bool _isWaitingHit;

    public StoneWandBoost(Player player, StoneWandBoostData data)
    {
        _player = player;
        _data = data;

        _collisionHandler = player.GetComponent<PlayerCollisionHandler>();
    }

    public void Activate()
    {
        _isWaitingHit = true;
    }

    public void Subscribe()
    {
        _collisionHandler.OnImpact += HandleImpact;
        _player.UI.EnableImage("RockWand");
    }

    public void Unsubscribe()
    {
        _collisionHandler.OnImpact -= HandleImpact;
        _player.UI.DisableImage("RockWand");
        _isWaitingHit = false;
    }

    private void HandleImpact(Vector3 position, ImpactData impactData)
    {
        if (!_isWaitingHit)
            return;

        _isWaitingHit = false;

        SpawnRock(position);

        Unsubscribe();
    }

    private void SpawnRock(Vector3 position)
    {
        if (_data.rockPrefab == null)
        {
            return;
        }

        Vector3 direction = _player.Rigidbody.velocity.normalized;

        if (direction == Vector3.zero)
            direction = _player.transform.forward;

        Vector3 spawnPosition = _player.transform.position + direction * 11f;

        GameObject rock = Object.Instantiate(
            _data.rockPrefab,
            spawnPosition,
            Quaternion.identity
        );

        var rockComponent = rock.GetComponent<RollingRock>();

        if (rockComponent == null)
        {
            Debug.LogError("RollingRock component missing!");
            return;
        }

        rockComponent.Init(direction, _player.ScoreSystem);

        AnimateSpawn(rock);
    }

    private void AnimateSpawn(GameObject rock)
    {
        rock.transform.localScale = Vector3.zero;
        Vector3 scale = new Vector3(8f, 8f, 8f);

        rock.transform
            .DOScale(scale, 0.25f)
            .SetEase(Ease.OutBack);
    }
}