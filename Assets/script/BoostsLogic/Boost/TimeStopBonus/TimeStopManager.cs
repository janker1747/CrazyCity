using System.Collections.Generic;
using UnityEngine;

public class TimeStopManager : MonoBehaviour
{
    [SerializeField] private List<Rigidbody> _rigidbodies = new List<Rigidbody>();

    public void Register(Rigidbody rb)
    {
        if (!_rigidbodies.Contains(rb))
            _rigidbodies.Add(rb);
    }

    public void Freeze()
    {
        foreach (var rb in _rigidbodies)
        {
            if (rb != null)
                rb.isKinematic = true;
        }
    }

    public void Unfreeze()
    {
        foreach (var rb in _rigidbodies)
        {
            if (rb != null)
                rb.isKinematic = false;
        }
    }
}