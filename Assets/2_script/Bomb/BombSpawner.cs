using UnityEngine;

public class BombSpawner : MonoBehaviour
{
    [SerializeField] private MainGameTimer _timer;
    [SerializeField] private CargoManager _cargoManager;

    [SerializeField] private BombPool _bombPool;
    [SerializeField] private float _maxDistanceAhead = 3f;
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private float _spawnOffsetY = 0.15f;
    [SerializeField] private float _spawnAngle = 45f;
    [SerializeField] private int _spawnAttempts = 8;
    [SerializeField] private float _checkRadius = 0.35f;
    [SerializeField] private float _minDistanceAhead = 1f;
    [SerializeField, Min(1)] private int _bombCount = 1;
    [SerializeField, Min(0)] private int _bombIncreasePerDelivery = 1;

    private Transform _player;
    private bool _deliverySubscribed;

    private void Start()
    {
        ResolvePlayer();
        SubscribeToDeliveries();
    }

    private void OnEnable()
    {
        if (_timer != null)
            _timer.TimeSpawnBomb += SpawnBombSmart;

        SubscribeToDeliveries();
    }

    private void OnDisable()
    {
        if (_timer != null)
            _timer.TimeSpawnBomb -= SpawnBombSmart;

        UnsubscribeFromDeliveries();
    }

    public void SpawnBombSmart()
    {
        if (_player == null && !ResolvePlayer())
            return;

        int bombsToSpawn = Mathf.Max(1, _bombCount);

        for (int i = 0; i < bombsToSpawn; i++)
            TrySpawnBomb();
    }

    private bool TrySpawnBomb()
    {
        Vector3 origin = _player.position + Vector3.up;
        int maxAttempts = _spawnAttempts;

        for (int i = 0; i < maxAttempts; i++)
        {
            float angle = Random.Range(-_spawnAngle, _spawnAngle);
            float distance = Random.Range(_minDistanceAhead, _maxDistanceAhead);

            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * _player.forward;

            if (Physics.Raycast(origin, direction, out RaycastHit hitForward, distance, _obstacleMask))
            {
                distance = Mathf.Max(hitForward.distance - 0.2f, 0.5f);
            }

            Vector3 spawnPos = origin + direction * distance;

            if (!Physics.Raycast(spawnPos + Vector3.up * 5f, Vector3.down,
                    out RaycastHit hitDown, 10f, _groundMask))
            {
                continue;
            }

            spawnPos = hitDown.point;
            spawnPos.y += _spawnOffsetY;

            // Проверяем, свободно ли место
            if (Physics.CheckSphere(spawnPos, _checkRadius, _obstacleMask))
            {
                continue;
            }

            GameObject temp = new GameObject("BombSpawnPoint");
            temp.transform.position = spawnPos;
            temp.transform.rotation = Quaternion.identity;

          
                _bombPool.SpawnBomb(temp.transform);

            Destroy(temp);

            return true;
        }

        return false;
    }

    private bool ResolvePlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        _player = playerObject != null ? playerObject.transform : null;
        return _player != null;
    }

    private void SubscribeToDeliveries()
    {
        if (_deliverySubscribed)
            return;

        if (_cargoManager == null)
            _cargoManager = FindObjectOfType<CargoManager>();

        if (_cargoManager == null)
            return;

        _cargoManager.DeliveryCompleted += OnDeliveryCompleted;
        _deliverySubscribed = true;
    }

    private void UnsubscribeFromDeliveries()
    {
        if (!_deliverySubscribed || _cargoManager == null)
            return;

        _cargoManager.DeliveryCompleted -= OnDeliveryCompleted;
        _deliverySubscribed = false;
    }

    private void OnDeliveryCompleted()
    {
        _bombCount += Mathf.Max(0, _bombIncreasePerDelivery);
    }
}
