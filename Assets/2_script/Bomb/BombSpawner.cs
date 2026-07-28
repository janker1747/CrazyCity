using UnityEngine;

public class BombSpawner : MonoBehaviour
{
    [SerializeField] private MainGameTimer _timer;

    [SerializeField] private BombPool _bombPool;
    [SerializeField] private float _maxDistanceAhead = 3f;
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private float _spawnOffsetY = 0.15f;
    [SerializeField] private float _spawnAngle = 45f;
    [SerializeField] private int _spawnAttempts = 8;
    [SerializeField] private float _checkRadius = 0.35f;
    [SerializeField] private float _minDistanceAhead = 1f;
    [SerializeField] private int _bombCount ;

    private Transform _player;

    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void OnEnable()
    {
        _timer.TimeSpawnBomb += SpawnBombSmart;
    }

    private void OnDisable()
    {
        _timer.TimeSpawnBomb -= SpawnBombSmart;
    }

    public void SpawnBombSmart()
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

            return;
        }
    }
}