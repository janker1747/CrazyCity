using System;
using ArcadeVP;
using UnityEngine;

public class PlayerAirController : MonoBehaviour
{
    [Header("Main")]
    [SerializeField] private ArcadeVehicleController _playerVehicle;
    [SerializeField] private Animator _animator;
    [SerializeField] private PlayerTrickLoadout _loadout;
    [SerializeField] private Player _player;

    private bool _isAirborne;

    private bool _isPerformingTrick;
    private TrickData _currentTrick;

    public event Action<bool> AirborneChanged;

    public bool IsAirborne => _isAirborne;

    private void OnEnable()
    {
        _playerVehicle.OnGrounded += Ground;
    }

    private void OnDisable()
    {
        _playerVehicle.OnGrounded -= Ground;
    }

    private void Update()
    {
        if (!_isAirborne)
            return;

        if (Input.GetKeyDown(KeyCode.Q))
            TryStartTrick(KeyCode.Q);

        if (Input.GetKeyDown(KeyCode.E))
            TryStartTrick(KeyCode.E);

        if (Input.GetKeyDown(KeyCode.Space))
            TryStartTrick(KeyCode.Space);
    }

    private void TryStartTrick(KeyCode key)
    {
        TrickData trick = _loadout.GetTrickForKey(key);
        if (trick != null)
            StartTrick(trick);
    }

    public void TryStartFirstTrick()
    {
        TryStartTrick(KeyCode.Q);
    }

    public void TryStartSecondTrick()
    {
        TryStartTrick(KeyCode.E);
    }

    private void Ground(bool isGrounded)
    {
        bool wasAirborne = _isAirborne;
        _isAirborne = !isGrounded;

        if (wasAirborne != _isAirborne)
            AirborneChanged?.Invoke(_isAirborne);

        if (_isAirborne)
            EnterAir();
        else
            ExitAir();
    }

    private void EnterAir() { }

    private void ExitAir()
    {
        if (_isPerformingTrick)
            FailTrick();

        _isPerformingTrick = false;
        _currentTrick = null;
    }

    private void StartTrick(TrickData trick)
    {
        if (_isPerformingTrick || !_isAirborne)
            return;

        _isPerformingTrick = true;
        _currentTrick = trick;

        _animator.SetTrigger(trick.animatorTrigger);
    }

    public void OnTrickAnimationEnd()
    {
        if (_isPerformingTrick && _isAirborne)
            CompleteTrick();
    }

    private void CompleteTrick()
    {
        int finalScore = _currentTrick.score;


        _player.ScoreSystem.AddScore(finalScore);

        _isPerformingTrick = false;
        _currentTrick = null;
    }

    private void FailTrick()
    {
    }
}
