using UnityEngine;
using _2_script;

public class SpawnedWorldObject : MonoBehaviour
{
    public MapGrids.Cell Cell { get; private set; }
    public GameObject SourcePrefab { get; private set; }
    public WorldObjectSpawner.SpawnGroup SpawnGroup { get; private set; }
    public WorldObjectSpawner Spawner { get; private set; }

    public int PrefabId { get; private set; }

    public void Initialize(
        MapGrids.Cell targetCell,
        WorldObjectSpawner targetSpawner,
        GameObject prefab,
        WorldObjectSpawner.SpawnGroup group)
    {
        Cell = targetCell;
        Spawner = targetSpawner;
        SourcePrefab = prefab;
        SpawnGroup = group;

        PrefabId = prefab != null ? prefab.GetInstanceID() : 0;
    }

    public void ReleaseCell()
    {
        if (Cell == null)
            return;

        Cell.occupied = false;
        Cell = null;
    }
}