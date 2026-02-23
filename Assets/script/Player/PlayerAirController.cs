using ArcadeVP;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class PlayerAirController : MonoBehaviour
{
    [Header("Main")]
    [SerializeField] private ArcadeVehicleController _playerVehicle;
    [SerializeField] private Animator _animator;
    [SerializeField] private PlayerTrickLoadout _loadout;
    [SerializeField] private ScoreUI scoreUI;

    [Header("Trick Prop")]
    [SerializeField] private TrickProp _trickProp;
    [SerializeField] private float _propTorqueForce = 50f;
    [SerializeField] private float _launchAngle = 45f;
    [SerializeField] private float _launchSpeed = 40f;

    private bool _isAirborne;

    private bool _isPerformingTrick;
    private TrickData _currentTrick;

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

    private void Ground(bool isGrounded)
    {
        _isAirborne = !isGrounded;

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

        Debug.Log($"Трюк {_currentTrick.trickName} выполнен! Очки: {finalScore}");

        scoreUI.AddScore(finalScore);

        _isPerformingTrick = false;
        _currentTrick = null;
    }

    private void FailTrick()
    {
        Debug.Log($"Трюк {_currentTrick.trickName} провален! Штраф.");
    }

    public void SpawnTrickProp()
    {
        if (_trickProp == null)
            return;

        _trickProp.transform.parent = null;
        _trickProp.gameObject.SetActive(true);

        Rigidbody rb = _trickProp.GetComponent<Rigidbody>();
        if (rb == null)
            return;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 launchDirection = Quaternion.Euler(-_launchAngle, 0f, 0f) * transform.forward;

        float power = _launchSpeed; 

        rb.AddForce(launchDirection * power, ForceMode.Impulse);

        rb.AddTorque(transform.right * _propTorqueForce, ForceMode.Impulse);
    }

}
