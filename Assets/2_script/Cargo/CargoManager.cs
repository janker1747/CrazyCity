using System.Collections.Generic;
using _2_script;
using UnityEngine;

public class CargoManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform deliverySpawnPoint;
    [SerializeField] private DeliveryPoint deliveryPointPrefab;

    private DeliveryPoint activeDeliveryPoint;
    private MapGrids.Cell activeCell;

    public DeliveryPoint ActiveDeliveryPoint => activeDeliveryPoint;

    public DeliveryPoint CreateDeliveryPointForPlayer(Vector3 playerPosition, Player player)
    {
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
