using System.Collections.Generic;
using _2_script;
using UnityEngine;

public class CargoManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform deliverySpawnPoint;
    [SerializeField] private DeliveryPoint deliveryPointPrefab;

    [Header("Delivery Distance")]
    [SerializeField, Min(0f)] private float minDeliveryDistance = 80f;
    [SerializeField, Min(0f)] private float maxDeliveryDistance = 140f;
    [SerializeField, Min(0f)] private float surfaceOffset = 0.2f;

    private DeliveryPoint activeDeliveryPoint;
    private MapGrids.Cell activeCell;

    public DeliveryPoint ActiveDeliveryPoint => activeDeliveryPoint;

    public DeliveryPoint CreateDeliveryPointForPlayer(Vector3 playerPosition, Player player)
    {
        if (player == null)
        {
            Debug.LogWarning($"{nameof(CargoManager)} on {name}: cannot create delivery point without a player.");
            return null;
        }

        if (deliverySpawnPoint == null)
        {
            Debug.LogWarning($"{nameof(CargoManager)} on {name}: Delivery spawn point is not assigned.");
            return null;
        }

        if (deliveryPointPrefab == null)
        {
            Debug.LogWarning($"{nameof(CargoManager)} on {name}: DeliveryPoint prefab is not assigned.");
            return null;
        }

        if (activeDeliveryPoint != null)
            return activeDeliveryPoint;

        activeDeliveryPoint = Instantiate(
            deliveryPointPrefab,
            deliverySpawnPoint.position,
            deliverySpawnPoint.rotation);

        activeDeliveryPoint.Init(player);

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
}
