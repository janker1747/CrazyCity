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

        [Min(0)]
        public int spawnAmount = 1;

        [Min(0f)]
        public float spawnChance = 1f;

        public bool randomRotation = true;

        public List<GameObject> prefabs = new();
    }

    [Header("References")]
    [SerializeField] private MainGameTimer timer;
    [SerializeField] private MapGrids mapGrid;

    [Header("Spawn")]
    [SerializeField] private float spawnInterval = 30f;

    [SerializeField]
    private List<SpawnGroup> spawnGroups = new();

    [Header("Settings")]
    [SerializeField] private bool spawnOnStart;

    [SerializeField]
    private Transform spawnedParent;

    private readonly List<MapGrids.Cell> freeCells = new();

    private float nextSpawnTime;
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

        if (mapGrid == null)
            mapGrid = FindObjectOfType<MapGrids>();

        if (timer == null)
            timer = FindObjectOfType<MainGameTimer>();

        nextSpawnTime = GetCurrentTimerValue() - spawnInterval;
    }

    private void HandleTimeChanged(float currentTime)
    {
        if (timer == null)
            return;

        if (timer.Direction != MainGameTimer.TimerDirection.Countdown)
        {
            if (currentTime >= nextSpawnTime + spawnInterval)
            {
                nextSpawnTime = currentTime;
                SpawnAll();
            }

            return;
        }

        if (currentTime <= nextSpawnTime)
        {
            nextSpawnTime -= spawnInterval;
            SpawnAll();
        }
    }

    private float GetCurrentTimerValue()
    {
        if (timer == null)
            return 0f;

        return timer.CurrentTime;
    }

    [ContextMenu("Spawn All")]
    public void SpawnAll()
    {
        if (mapGrid == null)
        {
            Debug.LogWarning(
                $"{nameof(WorldObjectSpawner)}: MapGrid is missing.");
            return;
        }

        CacheFreeCells();

        for (int i = 0; i < spawnGroups.Count; i++)
        {
            SpawnGroup group = spawnGroups[i];

            if (group == null)
                continue;

            if (group.prefabs == null || group.prefabs.Count == 0)
                continue;

            if (Random.value > group.spawnChance)
                continue;

            for (int j = 0; j < group.spawnAmount; j++)
            {
                SpawnFromGroup(group);
            }
        }
    }

    private void SpawnFromGroup(SpawnGroup group)
    {
        if (freeCells.Count == 0)
            return;

        GameObject prefab =
            group.prefabs[
                Random.Range(0, group.prefabs.Count)];

        if (prefab == null)
            return;

        int cellIndex = Random.Range(0, freeCells.Count);

        MapGrids.Cell cell = freeCells[cellIndex];

        freeCells.RemoveAt(cellIndex);

        cell.occupied = true;

        Quaternion rotation =
            group.randomRotation
                ? Quaternion.Euler(
                    0f,
                    Random.Range(0f, 360f),
                    0f)
                : Quaternion.identity;

        rotation =
            Quaternion.FromToRotation(
                Vector3.up,
                cell.normal) * rotation;

        GameObject spawnedObject =
            Instantiate(
                prefab,
                cell.position+ new Vector3(0f, 2f, 0f),
                rotation,
                spawnedParent);

        RegisterSpawnedObject(spawnedObject, cell);
    }

    private void RegisterSpawnedObject(
        GameObject spawnedObject,
        MapGrids.Cell cell)
    {
        if (spawnedObject == null)
            return;

        SpawnedWorldObject spawned =
            spawnedObject.GetComponent<SpawnedWorldObject>();

        if (spawned == null)
            spawned = spawnedObject.AddComponent<SpawnedWorldObject>();

        spawned.Initialize(cell);
    }

    private void CacheFreeCells()
    {
        freeCells.Clear();

        IReadOnlyList<MapGrids.Cell> cells = mapGrid.Cells;

        for (int i = 0; i < cells.Count; i++)
        {
            MapGrids.Cell cell = cells[i];

            if (cell == null)
                continue;

            if (cell.occupied)
                continue;

            freeCells.Add(cell);
        }
    }
}