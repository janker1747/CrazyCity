using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCollisionHandler : MonoBehaviour
{
    [SerializeField] private float _maxValue;
    [SerializeField] private float _minValue;
    [SerializeField] private ForceMode _forceMode = ForceMode.Impulse; // Режим силы

    private float _force;

    private void OnCollisionEnter(Collision collision)
    {
        IKnockbackable knockbackable = collision.gameObject.GetComponent<IKnockbackable>();

        if (knockbackable != null)
        {
            ApplyKnockback(collision);
        }
    }

    private void ApplyKnockback(Collision collision)
    {
        float knockbackForce = Random.Range(_minValue, _maxValue);

        Vector3 direction = (collision.transform.position - transform.position).normalized;

        Vector3 force = direction * knockbackForce + Vector3.up * (knockbackForce * 0.5f);

        collision.rigidbody.AddForce(force, _forceMode);
    }
}