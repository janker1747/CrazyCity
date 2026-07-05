using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using _2_script;

public class WorldObjectSpawner : MonoBehaviour
{
    [Serializable]
    public class SpawnGroup
    {
        public string id;

        [Min(0)] public int targetAmount = 5;
        [Min(0)] public int preloadAmount = 10;
        [Min(0f)] public float spawnChance = 1f;
        [Min(0f)] public float spawnInterval = 30f;

        public bool randomRotation = true;
        public Transform parent;
        public List<GameObject> prefabs = new();

        [HideInInspector] public float nextSpawnTime;
        [HideInInspector] public int activeCount;
    }

    [Header("References")]
    [SerializeField] private MainGameTimer timer;
    [SerializeField] private MapGrids mapGrids;

    [Header("Groups")]
    [SerializeField] private List<SpawnGroup> spawnGroups;

    [Header("Settings")]
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private Transform globalSpawnParent;

    private readonly List<MapGrids.Cell> freeCells = new();

    // 🔥 ключ: prefab reference (самый стабильный вариант)
    private readonly Dictionary<GameObject, Queue<GameObject>> pools = new();

    private bool initialized;

    private void Start()
    {
        Initialize();

        if (spawnOnStart)
            SpawnAll();
    }

    private void OnEnable()
    {
        if (timer != null)
            timer.TimeChanged += HandleTimeChanged;
    }

    private void OnDisable()
    {
        if (timer != null)
            timer.TimeChanged -= HandleTimeChanged;
    }

    private void Initialize()
    {
        if (initialized)
            return;

        initialized = true;

        if (mapGrids == null)
            mapGrids = FindObjectOfType<MapGrids>();

        if (timer == null)
            timer = FindObjectOfType<MainGameTimer>();

        float currentTime = timer != null ? timer.CurrentTime : 0f;

        foreach (var g in spawnGroups)
        {
            if (g == null) continue;

            g.nextSpawnTime = currentTime + g.spawnInterval;
            PrewarmGroup(g);
        }
    }

    private void PrewarmGroup(SpawnGroup group)
    {
        foreach (var prefab in group.prefabs)
        {
            if (prefab == null) continue;

            Queue<GameObject> pool = GetPool(prefab);

            for (int i = 0; i < group.preloadAmount; i++)
            {
                GameObject obj = Instantiate(prefab, globalSpawnParent);
                obj.SetActive(false);

                var spawned = obj.GetComponent<SpawnedWorldObject>();
                if (spawned == null)
                    spawned = obj.AddComponent<SpawnedWorldObject>();

                spawned.Initialize(null, this, prefab, group);

                pool.Enqueue(obj);
            }
        }
    }

    private void HandleTimeChanged(float currentTime)
    {
        foreach (var group in spawnGroups)
        {
            if (group == null) continue;
            if (group.spawnInterval <= 0f) continue;
            if (currentTime < group.nextSpawnTime) continue;

            group.nextSpawnTime = currentTime + group.spawnInterval;

            int missing = group.targetAmount - group.activeCount;
            if (missing <= 0) continue;

            CacheFreeCells();

            for (int i = 0; i < missing; i++)
            {
                if (Random.value > group.spawnChance)
                    continue;

                SpawnFromGroup(group);
            }
        }
    }

    public void SpawnAll()
    {
        foreach (var group in spawnGroups)
        {
            if (group == null) continue;

            int missing = group.targetAmount - group.activeCount;
            if (missing <= 0) continue;

            CacheFreeCells();

            for (int i = 0; i < missing; i++)
                SpawnFromGroup(group);
        }
    }

    private void SpawnFromGroup(SpawnGroup group)
    {
        if (freeCells.Count == 0) return;
        if (group.prefabs == null || group.prefabs.Count == 0) return;

        GameObject prefab = group.prefabs[Random.Range(0, group.prefabs.Count)];

        int index = Random.Range(0, freeCells.Count);
        MapGrids.Cell cell = freeCells[index];
        freeCells.RemoveAt(index);

        cell.occupied = true;

        Quaternion rotation = group.randomRotation
            ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)
            : Quaternion.identity;

        rotation = Quaternion.FromToRotation(Vector3.up, cell.normal) * rotation;

        GameObject obj = GetFromPool(prefab);

        Transform parent = group.parent != null ? group.parent : globalSpawnParent;

        obj.transform.SetParent(parent);
        obj.transform.SetPositionAndRotation(cell.position + Vector3.up * 2f, rotation);

        ResetPhysics(obj);

        var spawned = obj.GetComponent<SpawnedWorldObject>();
        spawned.Initialize(cell, this, prefab, group);

        obj.SetActive(true);

        group.activeCount++;
    }

    public void ReturnSpawnedObject(SpawnedWorldObject obj)
    {
        if (obj == null) return;

        obj.ReleaseCell();

        GameObject go = obj.gameObject;
        go.SetActive(false);
        go.transform.SetParent(globalSpawnParent);

        ResetPhysics(go);

        if (obj.SourcePrefab == null)
        {
            Destroy(go);
            return;
        }

        GetPool(obj.SourcePrefab).Enqueue(go);

        if (obj.SpawnGroup != null)
            obj.SpawnGroup.activeCount =
                Mathf.Max(0, obj.SpawnGroup.activeCount - 1);
    }

    private GameObject GetFromPool(GameObject prefab)
    {
        Queue<GameObject> pool = GetPool(prefab);

        while (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();

            if (obj == null) continue;
            if (obj.activeSelf) continue;

            return obj;
        }

        // 🔥 если пул пуст — создаём НОВЫЙ
        GameObject created = Instantiate(prefab, globalSpawnParent);

        var spawned = created.GetComponent<SpawnedWorldObject>();
        if (spawned == null)
            spawned = created.AddComponent<SpawnedWorldObject>();

        spawned.Initialize(null, this, prefab, null);

        return created;
    }

    private Queue<GameObject> GetPool(GameObject prefab)
    {
        if (!pools.TryGetValue(prefab, out var pool))
        {
            pool = new Queue<GameObject>();
            pools.Add(prefab, pool);
        }

        return pool;
    }

    private void ResetPhysics(GameObject target)
    {
        foreach (var rb in target.GetComponentsInChildren<Rigidbody>())
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }
    }

    private void CacheFreeCells()
    {
        freeCells.Clear();

        foreach (var c in mapGrids.Cells)
        {
            if (c == null) continue;
            if (c.occupied) continue;

            freeCells.Add(c);
        }
    }
}