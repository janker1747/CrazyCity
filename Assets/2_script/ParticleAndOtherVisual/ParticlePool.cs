using System.Collections.Generic;
using UnityEngine;

public class ParticlePool : MonoBehaviour
{
    private struct ActiveParticle
    {
        public ParticleSystem prefab;
        public ParticleSystem instance;
    }

    [SerializeField, Min(1)] private int activeCapacity = 8;

    private readonly Dictionary<ParticleSystem, Queue<ParticleSystem>> pool = new();
    private readonly List<ActiveParticle> activeParticles = new();

    private void Awake()
    {
        if (activeParticles.Capacity < activeCapacity)
            activeParticles.Capacity = activeCapacity;
    }

    private void LateUpdate()
    {
        for (int i = activeParticles.Count - 1; i >= 0; i--)
        {
            ParticleSystem particle = activeParticles[i].instance;

            if (particle != null &&
                (particle.isPlaying || particle.IsAlive(true)))
            {
                continue;
            }

            ReturnActiveParticle(i);
        }
    }

    private void OnDisable()
    {
        for (int i = activeParticles.Count - 1; i >= 0; i--)
            ReturnActiveParticle(i);
    }

    public ParticleSystem PlayParticle(
        ParticleSystem prefab,
        Vector3 position,
        Quaternion rotation)
    {
        if (prefab == null)
            return null;

        ParticleSystem particle = GetParticle(prefab);
        particle.transform.SetPositionAndRotation(position, rotation);

        particle.gameObject.SetActive(true);
        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.Play(true);

        activeParticles.Add(new ActiveParticle
        {
            prefab = prefab,
            instance = particle
        });

        return particle;
    }

    private ParticleSystem GetParticle(ParticleSystem prefab)
    {
        Queue<ParticleSystem> prefabPool = GetPool(prefab);

        while (prefabPool.Count > 0)
        {
            ParticleSystem particle = prefabPool.Dequeue();
            if (particle != null)
                return particle;
        }

        ParticleSystem created = Instantiate(prefab, transform);
        created.gameObject.SetActive(false);
        return created;
    }

    private void ReturnActiveParticle(int activeIndex)
    {
        ActiveParticle activeParticle = activeParticles[activeIndex];
        ParticleSystem particle = activeParticle.instance;

        int lastIndex = activeParticles.Count - 1;
        activeParticles[activeIndex] = activeParticles[lastIndex];
        activeParticles.RemoveAt(lastIndex);

        if (particle == null || activeParticle.prefab == null)
            return;

        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.gameObject.SetActive(false);
        particle.transform.SetParent(transform, false);
        GetPool(activeParticle.prefab).Enqueue(particle);
    }

    private Queue<ParticleSystem> GetPool(ParticleSystem prefab)
    {
        if (!pool.TryGetValue(prefab, out Queue<ParticleSystem> prefabPool))
        {
            prefabPool = new Queue<ParticleSystem>();
            pool.Add(prefab, prefabPool);
        }

        return prefabPool;
    }
}
