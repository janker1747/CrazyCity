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

    private readonly List<int> freeCellIndices = new();

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

        InitializeFreeCellIndices();

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
        foreach (GameObject prefab in group.prefabs)
        {
            if (prefab == null)
                continue;

            Queue<GameObject> pool = GetPool(prefab);

            for (int i = 0; i < group.preloadAmount; i++)
            {
                GameObject obj = Instantiate(prefab, globalSpawnParent);
                obj.SetActive(false);

                SpawnedWorldObject spawned =
                    obj.GetComponent<SpawnedWorldObject>();

                if (spawned == null)
                    spawned = obj.AddComponent<SpawnedWorldObject>();

                spawned.Initialize(
                    null,
                    -1,
                    this,
                    prefab,
                    group
                );

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

            for (int i = 0; i < missing; i++)
                SpawnFromGroup(group);
        }
    }

    private void SpawnFromGroup(SpawnGroup group)
    {
        if (freeCellIndices.Count == 0)
            return;

        if (group.prefabs == null || group.prefabs.Count == 0)
            return;

        GameObject prefab =
            group.prefabs[Random.Range(0, group.prefabs.Count)];

        if (prefab == null)
            return;

        int randomListIndex =
            Random.Range(0, freeCellIndices.Count);

        int cellIndex =
            freeCellIndices[randomListIndex];

        int lastListIndex = freeCellIndices.Count - 1;
        freeCellIndices[randomListIndex] = freeCellIndices[lastListIndex];
        freeCellIndices.RemoveAt(lastListIndex);

        if (!mapGrids.TryOccupyCell(cellIndex, out MapGrids.Cell cell))
            return;

        Quaternion rotation = group.randomRotation
            ? Quaternion.Euler(
                0f,
                Random.Range(0f, 360f),
                0f)
            : Quaternion.identity;

        GameObject obj = GetFromPool(prefab);

        Transform parent = group.parent != null
            ? group.parent
            : globalSpawnParent;

        obj.transform.SetParent(parent);

        obj.transform.SetPositionAndRotation(
            cell.position + Vector3.up * 2f,
            rotation
        );

        SpawnedWorldObject spawned =
            obj.GetComponent<SpawnedWorldObject>();

        if (spawned == null)
            spawned = obj.AddComponent<SpawnedWorldObject>();

        spawned.ResetPhysicsState();
        spawned.Initialize(
            mapGrids,
            cellIndex,
            this,
            prefab,
            group
        );

        obj.SetActive(true);

        group.activeCount++;
    }

    public void ReturnSpawnedObject(SpawnedWorldObject obj)
    {
        if (obj == null) return;

        int releasedCellIndex = obj.ReleaseCell();
        if (releasedCellIndex >= 0)
            freeCellIndices.Add(releasedCellIndex);

        GameObject go = obj.gameObject;
        go.SetActive(false);
        go.transform.SetParent(globalSpawnParent);

        obj.ResetPhysicsState();

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

        spawned.Initialize(
            null,
            -1,
            this,
            prefab,
            null);

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

    private void InitializeFreeCellIndices()
    {
        freeCellIndices.Clear();

        if (mapGrids == null)
            return;

        int cellCount = mapGrids.CellCount;
        if (freeCellIndices.Capacity < cellCount)
            freeCellIndices.Capacity = cellCount;

        for (int i = 0; i < cellCount; i++)
        {
            if (!mapGrids.TryGetCell(i, out MapGrids.Cell cell) ||
                cell.occupied)
            {
                continue;
            }

            freeCellIndices.Add(i);
        }
    }
}
