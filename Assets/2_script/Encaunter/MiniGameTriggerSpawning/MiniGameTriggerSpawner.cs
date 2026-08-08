using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using _2_script;

/// <summary>
/// Spawns pooled mini-game triggers only at spawn points with ground below.
/// </summary>
[DisallowMultipleComponent]
public sealed class MiniGameTriggerSpawner : MonoBehaviour
{
    [Header("Pools and positions")]
    [SerializeField] private MiniGameTriggerPool triggerPool;
    [SerializeField, HideInInspector, FormerlySerializedAs("triggerPools")]
    private List<MiniGameTriggerPool> legacyPools =
        new List<MiniGameTriggerPool>();
    [SerializeField] private MapGrids mapGrids;
    [Tooltip("Used only when Map Grids is not assigned or has no baked cells.")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform spawnedObjectsParent;

    [Header("Ground check")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField, Min(0f)] private float raycastStartHeight = 50f;
    [SerializeField, Min(0.1f)] private float raycastDistance = 100f;
    [Tooltip("How far above the ground surface an encounter is placed.")]
    [SerializeField, Min(0f), FormerlySerializedAs("groundOffset")]
    private float spawnHeightOffset = 1.05f;
    [SerializeField, Min(1)] private int maxGroundChecksPerAttempt = 32;

    [Header("Spawn schedule")]
    [SerializeField, Min(1)] private int maxActiveTriggers = 1;
    [SerializeField] private bool spawnOnEnable = true;
    [SerializeField, Min(0f)] private float initialDelay;
    [SerializeField, Min(0.1f)] private float spawnInterval = 15f;
    [SerializeField, Min(0.1f)] private float failedSpawnRetryDelay = 2f;
    [SerializeField, Min(0f)] private float minimumTriggerDistance = 12f;

    private readonly HashSet<MiniGameTrigger> activeTriggers = new HashSet<MiniGameTrigger>();
    private readonly Dictionary<MiniGameTrigger, int> occupiedCells =
        new Dictionary<MiniGameTrigger, int>();
    private readonly List<MiniGameTrigger> inactiveTriggers =
        new List<MiniGameTrigger>();
    private readonly List<MiniGameTrigger> legacyPrefabs =
        new List<MiniGameTrigger>();
    private readonly RaycastHit[] groundHits = new RaycastHit[1];
    private float nextSpawnTime;

    public int ActiveTriggerCount => activeTriggers.Count;

    private void OnEnable()
    {
        ResolveTriggerPool();

        if (mapGrids == null)
            mapGrids = FindObjectOfType<MapGrids>();

        RemoveInactiveTriggers();
        foreach (MiniGameTrigger trigger in activeTriggers)
        {
            if (trigger != null)
                trigger.Resolved += OnTriggerResolved;
        }

        nextSpawnTime = Time.time + initialDelay;

        if (spawnOnEnable && initialDelay <= 0f)
            TrySpawnTrigger();
    }

    private void OnDisable()
    {
        foreach (MiniGameTrigger trigger in activeTriggers)
        {
            if (trigger != null)
                trigger.Resolved -= OnTriggerResolved;
        }

    }

    private void OnDestroy()
    {
        ReleaseAllOccupiedCells();
        activeTriggers.Clear();
    }

    private void Update()
    {
        RemoveInactiveTriggers();

        if (activeTriggers.Count >= maxActiveTriggers || Time.time < nextSpawnTime)
            return;

        TrySpawnTrigger();
    }

    /// <summary>Can be invoked from inspector events or other gameplay code.</summary>
    public bool TrySpawnTrigger()
    {
        if (activeTriggers.Count >= maxActiveTriggers)
            return false;

        if (triggerPool == null)
        {
            Debug.LogError($"{nameof(MiniGameTriggerSpawner)} on '{name}' has no trigger pool.", this);
            enabled = false;
            return false;
        }

        if (!TryGetGroundSpawnPose(
                out Vector3 position,
                out Quaternion rotation,
                out int occupiedCellIndex))
        {
            nextSpawnTime = Time.time + failedSpawnRetryDelay;
            return false;
        }

        MiniGameTrigger trigger = triggerPool.SpawnRandom(position, rotation);
        if (trigger == null)
        {
            ReleaseCell(occupiedCellIndex);
            nextSpawnTime = Time.time + failedSpawnRetryDelay;
            return false;
        }

        if (spawnedObjectsParent != null)
            trigger.transform.SetParent(spawnedObjectsParent, true);

        trigger.Resolved += OnTriggerResolved;
        activeTriggers.Add(trigger);
        if (occupiedCellIndex >= 0)
            occupiedCells[trigger] = occupiedCellIndex;

        nextSpawnTime = Time.time + spawnInterval;
        return true;
    }

    private bool TryGetGroundSpawnPose(
        out Vector3 position,
        out Quaternion rotation,
        out int occupiedCellIndex)
    {
        position = default;
        rotation = default;
        occupiedCellIndex = -1;

        if (mapGrids != null && mapGrids.CellCount > 0)
            return TryGetMapGridSpawnPose(out position, out rotation, out occupiedCellIndex);

        if (spawnPoints == null || spawnPoints.Length == 0)
            return false;

        int startIndex = Random.Range(0, spawnPoints.Length);
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform spawnPoint = spawnPoints[(startIndex + i) % spawnPoints.Length];
            if (spawnPoint == null)
                continue;

            if (TryGetGroundPoseAt(
                    spawnPoint.position,
                    spawnPoint.forward,
                    out position,
                    out rotation))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryGetMapGridSpawnPose(
        out Vector3 position,
        out Quaternion rotation,
        out int occupiedCellIndex)
    {
        position = default;
        rotation = default;
        occupiedCellIndex = -1;

        int cellCount = mapGrids.CellCount;
        int checksThisAttempt = Mathf.Min(cellCount, maxGroundChecksPerAttempt);
        int startIndex = Random.Range(0, cellCount);

        for (int i = 0; i < checksThisAttempt; i++)
        {
            int cellIndex = (startIndex + i) % cellCount;
            if (!mapGrids.TryOccupyCell(cellIndex, out MapGrids.Cell cell))
                continue;

            bool hasValidPose = TryGetGroundPoseAt(
                cell.position,
                Vector3.forward,
                out position,
                out rotation);

            if (!hasValidPose || IsTooCloseToActiveTrigger(position))
            {
                mapGrids.ReleaseCell(cellIndex);
                continue;
            }

            occupiedCellIndex = cellIndex;
            return true;
        }

        return false;
    }

    private bool TryGetGroundPoseAt(
        Vector3 candidatePosition,
        Vector3 forwardHint,
        out Vector3 position,
        out Quaternion rotation)
    {
        position = default;
        rotation = default;

        Vector3 rayOrigin = candidatePosition + Vector3.up * raycastStartHeight;
        int hitCount = Physics.RaycastNonAlloc(
            rayOrigin,
            Vector3.down,
            groundHits,
            raycastDistance,
            groundMask,
            QueryTriggerInteraction.Ignore);

        if (hitCount == 0)
            return false;

        RaycastHit groundHit = groundHits[0];
        position = groundHit.point + groundHit.normal * spawnHeightOffset;
        if (IsTooCloseToActiveTrigger(position))
            return false;

        Vector3 forward = Vector3.ProjectOnPlane(forwardHint, groundHit.normal);
        rotation = forward.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(forward.normalized, groundHit.normal)
            : Quaternion.FromToRotation(Vector3.up, groundHit.normal);
        return true;
    }

    private void ResolveTriggerPool()
    {
        if (triggerPool == null)
            triggerPool = GetComponent<MiniGameTriggerPool>();

        if (triggerPool == null)
            triggerPool = gameObject.AddComponent<MiniGameTriggerPool>();

        if (triggerPool.HasConfiguredPrefabs || legacyPools == null)
            return;

        legacyPrefabs.Clear();
        for (int i = 0; i < legacyPools.Count; i++)
        {
            MiniGameTriggerPool legacyPool = legacyPools[i];
            MiniGameTrigger legacyPrefab = legacyPool != null
                ? legacyPool.LegacyPrefab
                : null;

            if (legacyPrefab != null && !legacyPrefabs.Contains(legacyPrefab))
                legacyPrefabs.Add(legacyPrefab);
        }

        triggerPool.Configure(legacyPrefabs);
    }

    private void OnTriggerResolved(MiniGameTrigger trigger, bool success)
    {
        if (trigger != null)
            trigger.Resolved -= OnTriggerResolved;

        activeTriggers.Remove(trigger);
        if (occupiedCells.TryGetValue(trigger, out int cellIndex))
        {
            occupiedCells.Remove(trigger);
            ReleaseCell(cellIndex);
        }

        nextSpawnTime = Time.time + spawnInterval;
    }

    private bool IsTooCloseToActiveTrigger(Vector3 candidatePosition)
    {
        float minimumDistanceSqr = minimumTriggerDistance * minimumTriggerDistance;
        foreach (MiniGameTrigger activeTrigger in activeTriggers)
        {
            if (activeTrigger == null)
                continue;

            Vector3 offset = activeTrigger.transform.position - candidatePosition;
            offset.y = 0f;
            if (offset.sqrMagnitude < minimumDistanceSqr)
                return true;
        }

        return false;
    }

    private void ReleaseAllOccupiedCells()
    {
        foreach (KeyValuePair<MiniGameTrigger, int> pair in occupiedCells)
            ReleaseCell(pair.Value);

        occupiedCells.Clear();
    }

    private void ReleaseCell(int cellIndex)
    {
        if (mapGrids != null && cellIndex >= 0)
            mapGrids.ReleaseCell(cellIndex);
    }

    private void RemoveInactiveTriggers()
    {
        inactiveTriggers.Clear();
        foreach (MiniGameTrigger trigger in activeTriggers)
        {
            if (trigger == null || !trigger.gameObject.activeInHierarchy)
                inactiveTriggers.Add(trigger);
        }

        for (int i = 0; i < inactiveTriggers.Count; i++)
        {
            MiniGameTrigger trigger = inactiveTriggers[i];
            if (trigger != null)
                trigger.Resolved -= OnTriggerResolved;

            activeTriggers.Remove(trigger);
            if (occupiedCells.TryGetValue(trigger, out int cellIndex))
            {
                occupiedCells.Remove(trigger);
                ReleaseCell(cellIndex);
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        maxActiveTriggers = Mathf.Max(1, maxActiveTriggers);
        raycastStartHeight = Mathf.Max(0f, raycastStartHeight);
        raycastDistance = Mathf.Max(0.1f, raycastDistance);
        spawnHeightOffset = Mathf.Max(0f, spawnHeightOffset);
        maxGroundChecksPerAttempt = Mathf.Max(1, maxGroundChecksPerAttempt);
        initialDelay = Mathf.Max(0f, initialDelay);
        spawnInterval = Mathf.Max(0.1f, spawnInterval);
        failedSpawnRetryDelay = Mathf.Max(0.1f, failedSpawnRetryDelay);
        minimumTriggerDistance = Mathf.Max(0f, minimumTriggerDistance);
    }
#endif
}
