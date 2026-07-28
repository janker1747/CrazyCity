using System;
using UnityEngine;

public class PlayerCollisionHandler : MonoBehaviour
{
    [SerializeField] private float _impactForce;
    [SerializeField] private float baseForce;

    private bool PowerOn;
    private float _speed;

    public event Action<Vector3, ImpactData> OnImpact;
    public event Action OnPoliceImpact;

    public void SetSpeed(float speed)
    {
        _speed = speed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Player player = GetComponent<Player>();
        player?.NotifyCargoCollision(collision);

        _impactForce = baseForce * _speed;

        Rigidbody rb = collision.rigidbody;
        ImpactSource source = collision.gameObject.GetComponent<ImpactSource>();

        if (source == null)
            return;

        bool isPolice = collision.gameObject.CompareTag("Police");

        if (isPolice)
        {
            if (!PowerOn)
                return;
        }

        Vector3 hitPoint = collision.contacts[0].point;
        Vector3 direction = (collision.transform.position - transform.position).normalized;

        source.OnKnocked();

        rb.constraints = RigidbodyConstraints.None;
        rb.AddForce(direction * _impactForce, ForceMode.Impulse);

        OnImpact?.Invoke(hitPoint, source.Data);

        if (isPolice)
            OnPoliceImpact?.Invoke();
    }

    public void PowerCollisionOn()
    {
        PowerOn = true;
    }

    public void PowerCollisionOff()
    {
        PowerOn = false;
    }
}
