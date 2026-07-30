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
    [SerializeField] private Button _trickButton1;
    [SerializeField] private Button _trickButton2;

    private Player _player;
    private PlayerAirController _playerAirController;

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

        bool useMobileInput = Application.isMobilePlatform;
        BindMobileInput(player, useMobileInput);

        if (useMobileInput)
            BindTrickButtons(player);
        else
            SetTrickButtonsVisible(false);
    }

    private void BindMobileInput(Player player, bool useMobileInput)
    {
        if (player == null)
            return;

        PlayerMobileInputController inputController =
            player.GetComponent<PlayerMobileInputController>();

        GameObject mobileInputObject = FindSceneObject("MobileInput");
        if (mobileInputObject != null)
            mobileInputObject.SetActive(useMobileInput);

        if (!useMobileInput)
        {
            if (inputController != null)
                inputController.enabled = false;

            return;
        }

        if (inputController == null)
            inputController = player.gameObject.AddComponent<PlayerMobileInputController>();

        inputController.enabled = true;

        if (mobileInputObject == null)
        {
            Debug.LogWarning(
                $"{nameof(CarGameLoader)}: MobileInput UI was not found.");
            return;
        }

        Transform mobileInputRoot = mobileInputObject.transform;

        BindMobileButton(
            mobileInputRoot,
            "Left",
            inputController,
            MobileInputButton.InputAction.Left);

        BindMobileButton(
            mobileInputRoot,
            "Rigth",
            inputController,
            MobileInputButton.InputAction.Right);

        BindMobileButton(
            mobileInputRoot,
            "Gas",
            inputController,
            MobileInputButton.InputAction.Forward);

        BindMobileButton(
            mobileInputRoot,
            "Breake",
            inputController,
            MobileInputButton.InputAction.Back);

        BindMobileButton(
            mobileInputRoot,
            "WALL RIDE",
            inputController,
            MobileInputButton.InputAction.WallRide);
    }

    private GameObject FindSceneObject(string objectName)
    {
        Transform[] sceneTransforms = FindObjectsOfType<Transform>(true);

        for (int i = 0; i < sceneTransforms.Length; i++)
        {
            Transform sceneTransform = sceneTransforms[i];
            if (sceneTransform.gameObject.scene == gameObject.scene &&
                sceneTransform.name == objectName)
            {
                return sceneTransform.gameObject;
            }
        }

        return null;
    }

    private void BindMobileButton(
        Transform root,
        string buttonName,
        PlayerMobileInputController inputController,
        MobileInputButton.InputAction action)
    {
        Transform buttonTransform = root.Find(buttonName);
        if (buttonTransform == null)
        {
            Debug.LogWarning(
                $"{nameof(CarGameLoader)}: mobile button '{buttonName}' was not found.");
            return;
        }

        MobileInputButton inputButton =
            buttonTransform.GetComponent<MobileInputButton>();

        if (inputButton == null)
            inputButton = buttonTransform.gameObject.AddComponent<MobileInputButton>();

        inputButton.Configure(inputController, action);
    }

    private void BindTrickButtons(Player player)
    {
        UnbindTrickButtons();

        if (player == null)
            return;

        _playerAirController =
            player.GetComponentInChildren<PlayerAirController>(true);
        if (_playerAirController == null)
        {
            Debug.LogWarning(
                $"{nameof(CarGameLoader)}: {nameof(PlayerAirController)} was not found on the player.");
            SetTrickButtonsVisible(false);
            return;
        }

        _trickButton1 = FindSceneButton(_trickButton1, "TrickButton1");
        _trickButton2 = FindSceneButton(_trickButton2, "TrickButton2");

        if (_trickButton1 != null)
            _trickButton1.onClick.AddListener(_playerAirController.TryStartFirstTrick);
        else
            Debug.LogWarning($"{nameof(CarGameLoader)}: button 'TrickButton1' was not found.");

        if (_trickButton2 != null)
            _trickButton2.onClick.AddListener(_playerAirController.TryStartSecondTrick);
        else
            Debug.LogWarning($"{nameof(CarGameLoader)}: button 'TrickButton2' was not found.");

        _playerAirController.AirborneChanged += SetTrickButtonsVisible;
        SetTrickButtonsVisible(_playerAirController.IsAirborne);
    }

    private Button FindSceneButton(Button assignedButton, string buttonName)
    {
        if (assignedButton != null)
            return assignedButton;

        Button[] sceneButtons = FindObjectsOfType<Button>(true);

        for (int i = 0; i < sceneButtons.Length; i++)
        {
            Button button = sceneButtons[i];
            if (button.gameObject.scene == gameObject.scene &&
                button.name == buttonName)
            {
                return button;
            }
        }

        return null;
    }

    private void SetTrickButtonsVisible(bool isVisible)
    {
        SetTrickButtonVisible(_trickButton1, isVisible);
        SetTrickButtonVisible(_trickButton2, isVisible);
    }

    private static void SetTrickButtonVisible(Button button, bool isVisible)
    {
        if (button == null)
            return;

        button.interactable = isVisible;
        button.gameObject.SetActive(isVisible);
    }

    private void UnbindTrickButtons()
    {
        if (_playerAirController == null)
            return;

        _playerAirController.AirborneChanged -= SetTrickButtonsVisible;

        if (_trickButton1 != null)
            _trickButton1.onClick.RemoveListener(_playerAirController.TryStartFirstTrick);

        if (_trickButton2 != null)
            _trickButton2.onClick.RemoveListener(_playerAirController.TryStartSecondTrick);

        _playerAirController = null;
    }

    private void OnDestroy()
    {
        UnbindTrickButtons();
    }
}
