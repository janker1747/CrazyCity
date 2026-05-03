using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RollingRock : MonoBehaviour
{
    [SerializeField] private RockTrigger rockTrigger;

    [SerializeField] private float speed;
    [SerializeField] private float lifeTime = 10f;

    private Rigidbody _rb;
    private ScoreSystem _scoreSystem;
    private bool _isDying;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public void Init(Vector3 direction, ScoreSystem scoreSystem)
    {
        _scoreSystem = scoreSystem;

        _rb.AddForce(direction.normalized * speed, ForceMode.Impulse);
        _rb.AddTorque(Vector3.right * 10f, ForceMode.Impulse);

        // Передаём ScoreSystem в триггер
        rockTrigger.Init(scoreSystem, this);

        Invoke(nameof(DestroyWithAnimation), lifeTime);
    }

    private void DestroyWithAnimation()
    {
        if (_isDying)
            return;

        _isDying = true;

        _rb.isKinematic = true;

        transform
            .DOScale(Vector3.zero, 0.25f)
            .SetEase(Ease.InBack)
            .OnComplete(() => Destroy(gameObject));
    }

    public bool IsDying => _isDying;
}
