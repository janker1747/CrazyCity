using System.Collections.Generic;
using UnityEngine;

public class CargoManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MapGrid mapGrid;
    [SerializeField] private DeliveryPoint deliveryPointPrefab;

    [Header("Delivery Distance")]
    [SerializeField, Min(0f)] private float minDeliveryDistance = 80f;
    [SerializeField, Min(0f)] private float maxDeliveryDistance = 140f;
    [SerializeField, Min(0f)] private float surfaceOffset = 0.2f;

    private DeliveryPoint activeDeliveryPoint;
    private MapGrid.Cell activeCell;

    public DeliveryPoint ActiveDeliveryPoint => activeDeliveryPoint;

    public DeliveryPoint CreateDeliveryPointForPlayer(Vector3 playerPosition, Player player)
    {
        if (player == null)
        {
            Debug.LogWarning($"{nameof(CargoManager)} on {name}: cannot create delivery point without a player.");
            return null;
        }

        if (mapGrid == null)
        {
            Debug.LogWarning($"{nameof(CargoManager)} on {name}: MapGrid is not assigned.");
            return null;
        }

        if (deliveryPointPrefab == null)
        {
            Debug.LogWarning($"{nameof(CargoManager)} on {name}: DeliveryPoint prefab is not assigned.");
            return null;
        }

        if (activeDeliveryPoint != null)
        {
            Debug.LogWarning($"{nameof(CargoManager)} on {name}: active delivery point already exists.");
            return null;
        }

        MapGrid.Cell cell = ChooseRandomFreeCell(playerPosition);
        if (cell == null)
            return null;

        Vector3 normal = cell.normal.sqrMagnitude > 0.001f ? cell.normal.normalized : Vector3.up;
        Vector3 spawnPosition = cell.position + normal * surfaceOffset;
        Quaternion spawnRotation = Quaternion.FromToRotation(Vector3.up, normal);

        activeDeliveryPoint = Instantiate(deliveryPointPrefab, spawnPosition, spawnRotation);
        activeDeliveryPoint.Init(player);

        activeCell = cell;
        activeCell.occupied = true;

        return activeDeliveryPoint;
    }

    public void OnDeliveryFinished()
    {
        if (activeCell != null)
        {
            activeCell.occupied = false;
            activeCell = null;
        }

        if (activeDeliveryPoint != null)
            Destroy(activeDeliveryPoint.gameObject);

        activeDeliveryPoint = null;
    }

    private MapGrid.Cell ChooseRandomFreeCell(Vector3 playerPosition)
    {
        IReadOnlyList<MapGrid.Cell> cells = mapGrid.Cells;
        if (cells == null || cells.Count == 0)
        {
            Debug.LogWarning($"{nameof(CargoManager)} on {name}: MapGrid has no baked cells.");
            return null;
        }

        float minDistance = Mathf.Min(minDeliveryDistance, maxDeliveryDistance);
        float maxDistance = Mathf.Max(minDeliveryDistance, maxDeliveryDistance);

        if (!Mathf.Approximately(minDistance, minDeliveryDistance))
            Debug.LogWarning($"{nameof(CargoManager)} on {name}: min/max delivery distance were swapped for this selection.");

        float minSqrDistance = minDistance * minDistance;
        float maxSqrDistance = maxDistance * maxDistance;

        List<MapGrid.Cell> candidates = new List<MapGrid.Cell>();

        // Distance filtering is the temporary estimate for a 40-50 second route.
        foreach (MapGrid.Cell cell in cells)
        {
            if (cell == null || cell.occupied)
                continue;

            float sqrDistance = (cell.position - playerPosition).sqrMagnitude;
            if (sqrDistance < minSqrDistance || sqrDistance > maxSqrDistance)
                continue;

            candidates.Add(cell);
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning($"{nameof(CargoManager)} on {name}: no free delivery cells in distance range {minDistance}-{maxDistance}.");
            return null;
        }

        int index = Random.Range(0, candidates.Count);
        return candidates[index];
    }
}
