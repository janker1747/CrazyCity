using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

/// <summary>
/// One scene-level pool for every encounter trigger prefab. Keep this
/// component on a scene object, never on a prefab being spawned.
/// </summary>
[DisallowMultipleComponent]
public sealed class MiniGameTriggerPool : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private List<MiniGameTrigger> prefabs = new List<MiniGameTrigger>();
    [SerializeField, Min(0)] private int preloadPerPrefab = 2;

    // Allows the scene to migrate from the former one-pool-per-prefab setup.
    [SerializeField, FormerlySerializedAs("_prefab")]
    private MiniGameTrigger legacyPrefab;

    private readonly Dictionary<MiniGameTrigger, Queue<MiniGameTrigger>> available =
        new Dictionary<MiniGameTrigger, Queue<MiniGameTrigger>>();
    private readonly Dictionary<MiniGameTrigger, MiniGameTrigger> sources =
        new Dictionary<MiniGameTrigger, MiniGameTrigger>();

    private bool initialized;

    public bool HasConfiguredPrefabs => prefabs != null && prefabs.Count > 0;
    public MiniGameTrigger LegacyPrefab => legacyPrefab;

    private void Awake()
    {
        Initialize();
    }

    public void Configure(IReadOnlyList<MiniGameTrigger> sourcePrefabs)
    {
        if (initialized || sourcePrefabs == null)
            return;

        prefabs.Clear();
        for (int i = 0; i < sourcePrefabs.Count; i++)
        {
            MiniGameTrigger prefab = sourcePrefabs[i];
            if (prefab != null && !prefabs.Contains(prefab))
                prefabs.Add(prefab);
        }

        Initialize();
    }

    public MiniGameTrigger SpawnRandom(Vector3 position, Quaternion rotation)
    {
        Initialize();

        MiniGameTrigger sourcePrefab = GetRandomPrefab();
        return sourcePrefab == null
            ? null
            : Spawn(sourcePrefab, position, rotation);
    }

    public MiniGameTrigger Spawn(
        MiniGameTrigger sourcePrefab,
        Vector3 position,
        Quaternion rotation)
    {
        Initialize();
        if (sourcePrefab == null || !available.TryGetValue(sourcePrefab, out Queue<MiniGameTrigger> queue))
            return null;

        MiniGameTrigger trigger = GetAvailableTrigger(queue);
        if (trigger == null)
            trigger = CreateTrigger(sourcePrefab);

        trigger.transform.SetPositionAndRotation(position, rotation);
        trigger.ResetTrigger();
        trigger.gameObject.SetActive(true);
        return trigger;
    }

    private void Initialize()
    {
        if (initialized || prefabs == null || prefabs.Count == 0)
            return;

        initialized = true;
        for (int i = 0; i < prefabs.Count; i++)
        {
            MiniGameTrigger prefab = prefabs[i];
            if (prefab == null || available.ContainsKey(prefab))
                continue;

            Queue<MiniGameTrigger> queue = new Queue<MiniGameTrigger>();
            available.Add(prefab, queue);

            for (int preloadIndex = 0; preloadIndex < preloadPerPrefab; preloadIndex++)
                queue.Enqueue(CreateTrigger(prefab));
        }
    }

    private MiniGameTrigger CreateTrigger(MiniGameTrigger sourcePrefab)
    {
        MiniGameTrigger trigger = Instantiate(sourcePrefab, transform);
        trigger.gameObject.SetActive(false);
        trigger.Resolved -= OnTriggerResolved;
        trigger.Resolved += OnTriggerResolved;
        sources[trigger] = sourcePrefab;
        return trigger;
    }

    private MiniGameTrigger GetAvailableTrigger(Queue<MiniGameTrigger> queue)
    {
        while (queue.Count > 0)
        {
            MiniGameTrigger trigger = queue.Dequeue();
            if (trigger != null && !trigger.gameObject.activeInHierarchy)
                return trigger;
        }

        return null;
    }

    private MiniGameTrigger GetRandomPrefab()
    {
        if (prefabs == null || prefabs.Count == 0)
            return null;

        int startIndex = Random.Range(0, prefabs.Count);
        for (int i = 0; i < prefabs.Count; i++)
        {
            MiniGameTrigger prefab = prefabs[(startIndex + i) % prefabs.Count];
            if (prefab != null && available.ContainsKey(prefab))
                return prefab;
        }

        return null;
    }

    private void OnTriggerResolved(MiniGameTrigger trigger, bool success)
    {
        if (trigger == null || !sources.TryGetValue(trigger, out MiniGameTrigger sourcePrefab))
            return;

        trigger.ResetTrigger();
        trigger.gameObject.SetActive(false);
        available[sourcePrefab].Enqueue(trigger);
    }

    private void OnDestroy()
    {
        foreach (KeyValuePair<MiniGameTrigger, MiniGameTrigger> pair in sources)
        {
            if (pair.Key != null)
                pair.Key.Resolved -= OnTriggerResolved;
        }
    }
}
