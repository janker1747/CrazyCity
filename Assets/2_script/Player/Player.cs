using System;
using ArcadeVP;
using System.Collections.Generic;
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
    private float _bonusSpeed;
    private float _baseVehicleAcceleration;
    private float _baseVehicleGravity;
    private float _baseVehicleDownforce;
    private readonly List<float> _speedMultipliers = new List<float>();
    private readonly List<float> _accelerationMultipliers = new List<float>();
    private readonly List<float> _gravityMultipliers = new List<float>();
    private readonly List<float> _downforceMultipliers = new List<float>();

    public PlayerBoostSlot BoostSlot;
    public BoostSystem BoostSystem;

    public ScoreSystem ScoreSystem { get; private set; }
    public TimerUI Timer { get { return _UI.Timer; } }
    public UiPlayer UI { get { return _UI; } }
    public TimeStopManager Stoper { get { return _stopManager; } }
    public Rigidbody Rigidbody { get; private set; }
    public PlayerCollisionHandler PlayerCollision { get { return _collisionHandler; } }
    public ArcadeVehicleController VehicleController { get { return _vehicleController; } }
    public PlayerCargoModule CargoModule { get { EnsureCargoModule(); return _cargoModule; } }
    public float CurrentSpeed => Rigidbody != null ? Rigidbody.velocity.magnitude : 0f;
    public bool HasShield { get; private set; }
    public Cargo CurrentCargo { get { EnsureCargoModule(); return _cargoModule != null ? _cargoModule.CurrentCargo : null; } }
    public bool HasActiveCargo { get { EnsureCargoModule(); return _cargoModule != null && _cargoModule.HasActiveCargo; } }
    public int ActiveCargoCount { get { EnsureCargoModule(); return _cargoModule != null ? _cargoModule.ActiveCargoCount : 0; } }
    public bool CanTakeCargo { get { EnsureCargoModule(); return _cargoModule != null && _cargoModule.CanTakeCargo; } }

    private void Awake()
    {
        Rigidbody = GetComponent<Rigidbody>();

        BoostSlot = new PlayerBoostSlot();
        BoostSystem = new BoostSystem();
        ScoreSystem = new ScoreSystem();

        _scoreUI.Init(ScoreSystem);

        Speed = _vehicleController.MaxSpeed;
        _baseVehicleAcceleration = _vehicleController.accelaration;
        _baseVehicleGravity = _vehicleController.gravity;
        _baseVehicleDownforce = _vehicleController.downforce;

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
        _bonusSpeed += Speed;
        ApplyVehicleMultipliers();
    }

    public void EndBonusSpeed()
    {
        _bonusSpeed = 0f;
        ApplyVehicleMultipliers();
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

    public void AddSpeedMultiplier(float multiplier)
    {
        _speedMultipliers.Add(Mathf.Max(0f, multiplier));
        ApplyVehicleMultipliers();
    }

    public void RemoveSpeedMultiplier(float multiplier)
    {
        RemoveMultiplier(_speedMultipliers, multiplier);
        ApplyVehicleMultipliers();
    }

    public void AddAccelerationMultiplier(float multiplier)
    {
        _accelerationMultipliers.Add(Mathf.Max(0f, multiplier));
        ApplyVehicleMultipliers();
    }

    public void RemoveAccelerationMultiplier(float multiplier)
    {
        RemoveMultiplier(_accelerationMultipliers, multiplier);
        ApplyVehicleMultipliers();
    }

    public void AddGravityMultiplier(float multiplier)
    {
        _gravityMultipliers.Add(Mathf.Max(0f, multiplier));
        ApplyVehicleMultipliers();
    }

    public void RemoveGravityMultiplier(float multiplier)
    {
        RemoveMultiplier(_gravityMultipliers, multiplier);
        ApplyVehicleMultipliers();
    }

    public void AddDownforceMultiplier(float multiplier)
    {
        _downforceMultipliers.Add(Mathf.Max(0f, multiplier));
        ApplyVehicleMultipliers();
    }

    public void RemoveDownforceMultiplier(float multiplier)
    {
        RemoveMultiplier(_downforceMultipliers, multiplier);
        ApplyVehicleMultipliers();
    }

    public void NotifyCargoCollision(Collision collision)
    {
        EnsureCargoModule();
        _cargoModule?.NotifyPlayerCollision(collision);
    }

    public int ModifyCargoScoreDamage(int damage)
    {
        EnsureCargoModule();
        return _cargoModule != null ? _cargoModule.ModifyScoreDamage(damage) : damage;
    }

    public void NotifyCargoScoreDamage(int damage)
    {
        EnsureCargoModule();
        _cargoModule?.NotifyPlayerScoreDamage(damage);
    }

    private void ApplyVehicleMultipliers()
    {
        if (_vehicleController == null)
            return;

        _vehicleController.MaxSpeed = (Speed + _bonusSpeed) * Multiply(_speedMultipliers);
        _vehicleController.accelaration = _baseVehicleAcceleration * Multiply(_accelerationMultipliers);
        _vehicleController.gravity = _baseVehicleGravity * Multiply(_gravityMultipliers);
        _vehicleController.downforce = _baseVehicleDownforce * Multiply(_downforceMultipliers);
    }

    private float Multiply(List<float> multipliers)
    {
        float finalMultiplier = 1f;
        for (int i = 0; i < multipliers.Count; i++)
            finalMultiplier *= multipliers[i];

        return finalMultiplier;
    }

    private void RemoveMultiplier(List<float> multipliers, float multiplier)
    {
        float safeMultiplier = Mathf.Max(0f, multiplier);

        for (int i = multipliers.Count - 1; i >= 0; i--)
        {
            if (!Mathf.Approximately(multipliers[i], safeMultiplier))
                continue;

            multipliers.RemoveAt(i);
            return;
        }
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
