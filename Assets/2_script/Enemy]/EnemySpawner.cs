using UnityEngine;
using ArcadeVP;
using System;
using _2_script.Enemy_;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemyPool _pool;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private MainGameTimer _timer;

    [SerializeField] private Player _PoliceTarget;

    private int _lastSpawnMinute = -1;

    public event Action<Enemy> OnSpawn;

    public void SetTarget(Player player)
    {
        _PoliceTarget = player;
    }

    private void OnEnable()
    {
        _timer.TimeChanged += OnTimeChanged;
    }

    private void OnDisable()
    {
        _timer.TimeChanged -= OnTimeChanged;
    }

    private void OnTimeChanged(float currentTime)
    {
        int currentMinute = Mathf.FloorToInt(currentTime / 60f);

        if (currentMinute > _lastSpawnMinute)
        {
            _lastSpawnMinute = currentMinute;

            SpawnPoliceCar();
        }
    }

    public void SpawnPoliceCar()
    {
        if (_PoliceTarget == null)
            return;

        Enemy enemy = _pool.GetObject(_spawnPoint);
        enemy._policeAi
            .Initialize(_PoliceTarget.transform);

        OnSpawn?.Invoke(enemy);
    }
}
