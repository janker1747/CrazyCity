using System;
using ArcadeVP;
using TMPro;
using UnityEngine;


[RequireComponent(typeof(PlayerCollisionHandler))]
[RequireComponent(typeof(ArcadeVehicleController))]
[RequireComponent(typeof(UiPlayer))]
[RequireComponent(typeof(ScoreUI))]
public class Player : MonoBehaviour
{
    [SerializeField] private TimeStopManager _stopManager;
    [SerializeField] private CameraScoreFeedback _cameraFeedback;
    [SerializeField] private PlayerCollisionHandler _collisionHandler;
    [SerializeField] private ArcadeVehicleController _vehicleController;
    [SerializeField] private UiPlayer _UI;
    [SerializeField] private ScoreUI _scoreUI;

    private float Speed;

    public PlayerBoostSlot BoostSlot;
    public BoostSystem BoostSystem;

    public ScoreSystem ScoreSystem { get; private set; }
    public TimerUI Timer { get { return _UI.Timer; } }
    public UiPlayer UI { get { return _UI; } }
    public TimeStopManager Stoper { get { return _stopManager; } }
    public Rigidbody Rigidbody { get; private set; }
    public PlayerCollisionHandler PlayerCollision { get { return _collisionHandler; } }
    public bool HasShield { get; private set; }

    private void Awake()
    {
        Rigidbody = GetComponent<Rigidbody>();

        BoostSlot = new PlayerBoostSlot();
        BoostSystem = new BoostSystem();
        ScoreSystem = new ScoreSystem();

        _scoreUI.Init(ScoreSystem);

        Speed = _vehicleController.MaxSpeed;
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
}