using UnityEngine;
using _2_script;

public class SpawnedWorldObject : MonoBehaviour
{
    public MapGrids.Cell Cell { get; private set; }
    public int CellIndex { get; private set; } = -1;

    public GameObject SourcePrefab { get; private set; }
    public WorldObjectSpawner.SpawnGroup SpawnGroup { get; private set; }
    public WorldObjectSpawner Spawner { get; private set; }
    public MapGrids Grid { get; private set; }

    public int PrefabId { get; private set; }
    private Rigidbody[] cachedRigidbodies;

    public void Initialize(
        MapGrids targetGrid,
        int targetCellIndex,
        WorldObjectSpawner targetSpawner,
        GameObject prefab,
        WorldObjectSpawner.SpawnGroup group)
    {
        Grid = targetGrid;
        CellIndex = targetCellIndex;
        Cell = targetGrid != null &&
               targetGrid.TryGetCell(targetCellIndex, out MapGrids.Cell cell)
            ? cell
            : default;

        Spawner = targetSpawner;
        SourcePrefab = prefab;
        SpawnGroup = group;

        PrefabId = prefab != null ? prefab.GetInstanceID() : 0;
    }

    public int ReleaseCell()
    {
        if (Grid == null || CellIndex < 0)
            return -1;

        int releasedIndex = CellIndex;
        bool released = Grid.ReleaseCell(releasedIndex);

        CellIndex = -1;
        Cell = default;
        Grid = null;

        return released ? releasedIndex : -1;
    }

    public void ResetPhysicsState()
    {
        if (cachedRigidbodies == null)
            cachedRigidbodies = GetComponentsInChildren<Rigidbody>(true);

        for (int i = 0; i < cachedRigidbodies.Length; i++)
        {
            Rigidbody body = cachedRigidbodies[i];
            if (body == null)
                continue;

            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.Sleep();
        }
    }
}
