using System;
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
    public event Action DeliveryCompleted;
    public event Action CargoDelivered;

    private void Awake()
    {
        ChoiceContract.TryInitialize(this);
    }

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
        if (activeDeliveryPoint != null)
            Destroy(activeDeliveryPoint.gameObject);

        activeDeliveryPoint = null;
    }

    public void NotifySuccessfulDelivery()
    {
        DeliveryCompleted?.Invoke();
    }

    public void NotifyCargoDelivered()
    {
        CargoDelivered?.Invoke();
    }
}
