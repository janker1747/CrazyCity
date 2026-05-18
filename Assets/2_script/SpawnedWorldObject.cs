using UnityEngine;
using _2_script;

public class SpawnedWorldObject : MonoBehaviour
{
    private MapGrids.Cell cell;

    public void Initialize(MapGrids.Cell targetCell)
    {
        cell = targetCell;

        if (cell != null)
            cell.occupied = true;
    }

    private void OnDestroy()
    {
        if (cell != null)
            cell.occupied = false;
    }
}