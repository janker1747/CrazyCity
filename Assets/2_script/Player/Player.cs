using System;
using ArcadeVP;
using TMPro;
using UnityEngine;


[RequireComponent(typeof(PlayerCollisionHandler))]
[RequireComponent(typeof(ArcadeVehicleController))]
[RequireComponent(typeof(UiPlayer))]
[RequireComponent(typeof(ScoreUI))]
[RequireComponent(typeof(PlayerCargoModule))]
public class Player : MonoBehaviour
{
    [SerializeField] private TimeStopManager _stopManager;
    [SerializeField] private CameraScoreFeedback _cameraFeedback;
    [SerializeField] private PlayerCollisionHandler _collisionHandler;
    [SerializeField] private ArcadeVehicleController _vehicleController;
    [SerializeField] private UiPlayer _UI;
    [SerializeField] private ScoreUI _scoreUI;

    [Header("Cargo")]
    [SerializeField] private PlayerCargoModule _cargoModule;
    [SerializeField] private CargoManager _cargoManager;
    [SerializeField] private CargoArrowUI _cargoArrowUI;

    private float Speed;
    private float _baseVehicleGravity;

    public PlayerBoostSlot BoostSlot;
    public BoostSystem BoostSystem;

    public ScoreSystem ScoreSystem { get; private set; }
    public TimerUI Timer { get { return _UI.Timer; } }
    public UiPlayer UI { get { return _UI; } }
    public TimeStopManager Stoper { get { return _stopManager; } }
    public Rigidbody Rigidbody { get; private set; }
    public PlayerCollisionHandler PlayerCollision { get { return _collisionHandler; } }
    public bool HasShield { get; private set; }
    public Cargo CurrentCargo { get { EnsureCargoModule(); return _cargoModule != null ? _cargoModule.CurrentCargo : null; } }
    public bool CanTakeCargo { get { EnsureCargoModule(); return _cargoModule != null && _cargoModule.CanTakeCargo; } }

    private void Awake()
    {
        Rigidbody = GetComponent<Rigidbody>();

        BoostSlot = new PlayerBoostSlot();
        BoostSystem = new BoostSystem();
        ScoreSystem = new ScoreSystem();

        _scoreUI.Init(ScoreSystem);

        Speed = _vehicleController.MaxSpeed;
        _baseVehicleGravity = _vehicleController.gravity;

        EnsureCargoModule();
        if (_cargoModule != null)
            _cargoModule.Initialize(this, _cargoManager, _cargoArrowUI);
    }

    private void OnEnable()
    {
        _vehicleController.OnSpeedChanged += _UI.UpdateText;
        ScoreSystem.OnScoreAdded += _cameraFeedback.HandleAddScore;
        ScoreSystem.OnScoreRemoved += _cameraFeedback.HandleRemoveScore;
        _vehicleController.OnSpeedChanged += _collisionHandler.SetSpeed;
        BoostSlot.BoostPickUP += _UI.HandleBoost;
        _UI.OnButtonClick += UseBoost;
    }

    private void Update()
    {
        BoostSystem.Update(Time.deltaTime);
        _cargoModule?.Tick(Time.deltaTime);
    }

    public void UseBoost()
    {
        if (!BoostSlot.HasBoost)
            return;

        var data = BoostSlot.Take();

        var boost = data.Create(this);

        BoostSystem.ActivateBoost(boost);
    }

    private void OnDisable()
    {
        _vehicleController.OnSpeedChanged -= _UI.UpdateText;
        ScoreSystem.OnScoreAdded -= _cameraFeedback.HandleAddScore;
        ScoreSystem.OnScoreRemoved -= _cameraFeedback.HandleRemoveScore;
        _vehicleController.OnSpeedChanged -= _collisionHandler.SetSpeed;
        BoostSlot.BoostPickUP -= _UI.HandleBoost;
        _UI.OnButtonClick -= UseBoost;
    }

    public void AddSpeed(float Speed)
    {
        _vehicleController.MaxSpeed += Speed;
    }

    public void EndBonusSpeed()
    {
        _vehicleController.MaxSpeed = Speed;
    }

    public void RemoveScore(int amount)
    {
        ScoreSystem.MinusScore(amount);
    }

    public void EnableShield()
    {
        _UI.EnableImage("shield");
        HasShield = true;
    }

    public void ConsumeShield()
    {
        _UI.DisableImage("shield");
        HasShield = false;
    }

    public bool TryTakeCargo(Cargo cargo)
    {
        EnsureCargoModule();

        if (_cargoModule == null)
            return false;

        return _cargoModule.TryTakeCargo(cargo);
    }

    public void CompleteDelivery(bool success)
    {
        EnsureCargoModule();

        if (_cargoModule == null)
            return;

        _cargoModule.CompleteDelivery(success);
    }

    public void FailDelivery()
    {
        EnsureCargoModule();

        if (_cargoModule == null)
            return;

        _cargoModule.FailDelivery();
    }

    public void SetGravityMultiplier(float multiplier)
    {
        if (_vehicleController == null)
        {
            Debug.LogWarning($"{nameof(Player)} on {name}: cannot change gravity because ArcadeVehicleController is missing.");
            return;
        }

        float safeMultiplier = Mathf.Max(0f, multiplier);
        _vehicleController.gravity = _baseVehicleGravity * safeMultiplier;
    }

    private void EnsureCargoModule()
    {
        if (_cargoModule != null)
            return;

        _cargoModule = GetComponent<PlayerCargoModule>();

        if (_cargoModule == null)
        {
            Debug.LogWarning($"{nameof(Player)} on {name}: PlayerCargoModule was missing and was added at runtime.");
            _cargoModule = gameObject.AddComponent<PlayerCargoModule>();
        }
    }
}
