using System.Collections;
using System.Collections.Generic;
using Unity.Physics.Extensions;
using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [SerializeField] private float _force;
    [SerializeField] private UnityEngine.ForceMode _forceMode;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ApplyKnockback(other);
        }

    }

    private void ApplyKnockback(Collider collision)
    {
        float knockbackForce = _force ;

        Vector3 direction = Vector3.up;

        Vector3 force = direction * knockbackForce + Vector3.up * (knockbackForce * 0.5f);

        collision.GetComponent<Rigidbody>().AddForce(force, _forceMode);
    }
}
