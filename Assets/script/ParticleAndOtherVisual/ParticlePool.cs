using System.Collections.Generic;
using UnityEngine;

public class ParticlePool : MonoBehaviour
{
    private Dictionary<ParticleSystem, Queue<ParticleSystem>> _pool = new();

    public ParticleSystem GetParticle(ParticleSystem prefab)
    {
        if (!_pool.ContainsKey(prefab))
        {
            _pool[prefab] = new Queue<ParticleSystem>();
        }

        if (_pool[prefab].Count > 0)
        {
            return _pool[prefab].Dequeue();
        }

        return Instantiate(prefab);
    }

    public void ReturnParticle(ParticleSystem prefab, ParticleSystem particle)
    {
        particle.Stop();
        _pool[prefab].Enqueue(particle);
    }
}