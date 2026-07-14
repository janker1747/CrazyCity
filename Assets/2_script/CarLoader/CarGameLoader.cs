using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CarGameLoader : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private List<Transform> _spawnPlayerPoint;
    [SerializeField] private Player _fallbackPlayerPrefab;
    [SerializeField] private bool _dontTryLoad;

    [Header("Scene Systems")]
    [SerializeField] private TimeStopManager _timeStopManager;
    [SerializeField] private CargoManager _cargoManager;
    [SerializeField] private CargoUIController _cargoUIController;
    [SerializeField] private EnemySpawner _enemySpawner;
    [SerializeField] private ImpactSystem _impactSystem;
    [SerializeField] private CargoInventoryUI _cargoInventoryUI;
    [SerializeField] private PlayerHealthUI _playerHealthUI;

    [Header("Scene HUD")]
    [SerializeField] private TMP_Text _speedText;
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private Button _useBoostButton;
    [SerializeField] private Image _boostIcon;

    private Player _player;

    public Player Player => _player;

    private void Awake()
    {
        if (_dontTryLoad)
            return;

        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        Player playerPrefab = ResolvePlayerPrefab();

        if (playerPrefab == null)
        {
            Debug.LogError($"{nameof(CarGameLoader)}: selected player prefab is missing.");
            return;
        }

        Transform spawnPoint = GetSpawnPoint();

        if (spawnPoint == null)
        {
            Debug.LogError($"{nameof(CarGameLoader)}: no valid player spawn point is assigned.");
            return;
        }

        _player = Instantiate(
            playerPrefab,
            spawnPoint.position,
            spawnPoint.rotation);

        _player.name = playerPrefab.name;
        BindSceneToPlayer(_player);
    }

    private Player ResolvePlayerPrefab()
    {
        CarItemSO selectedCar = GameData.Instance.CarItem;

        if (selectedCar == null && CarSelectionManager.Instance != null)
            selectedCar = CarSelectionManager.Instance.GetCurrentCar();

        if (selectedCar != null && selectedCar.PlayerPrefab != null)
            return selectedCar.PlayerPrefab;

        return _fallbackPlayerPrefab;
    }

    private Transform GetSpawnPoint()
    {
        if (_spawnPlayerPoint == null || _spawnPlayerPoint.Count == 0)
            return null;

        int startIndex = Random.Range(0, _spawnPlayerPoint.Count);

        for (int i = 0; i < _spawnPlayerPoint.Count; i++)
        {
            Transform point = _spawnPlayerPoint[(startIndex + i) % _spawnPlayerPoint.Count];
            if (point != null)
                return point;
        }

        return null;
    }

    private void BindSceneToPlayer(Player player)
    {
        player.ConfigureSceneDependencies(
            _timeStopManager,
            _cargoManager,
            _cargoUIController);

        player.UI?.BindSceneUI(_speedText, _useBoostButton, _boostIcon);
        player.ScoreView?.SetText(_scoreText);

        _enemySpawner?.SetTarget(player);
        _impactSystem?.SetPlayer(player);
        _cargoInventoryUI?.SetCargoModule(player.CargoModule);
        _playerHealthUI?.SetPlayerHealth(player.Health);
    }
}
