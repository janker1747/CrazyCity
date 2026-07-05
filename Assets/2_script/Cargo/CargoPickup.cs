using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CargoPickup : MonoBehaviour
{
    private static readonly List<CargoPickup> activePickups = new();

    [SerializeField] private Cargo cargoData;

    private Collider pickupCollider;

    public static IReadOnlyList<CargoPickup> ActivePickups => activePickups;
    public Cargo CargoData => cargoData;
    public bool IsAvailable => isActiveAndEnabled && cargoData != null;

    private void Awake()
    {
        pickupCollider = GetComponent<Collider>();

        if (!pickupCollider.isTrigger)
        {
            pickupCollider.isTrigger = true;
        }
    }

    private void OnEnable()
    {
        if (!activePickups.Contains(this))
            activePickups.Add(this);
    }

    private void OnDisable()
    {
        activePickups.Remove(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        Player player = other.GetComponentInParent<Player>();
        
        if (player == null)
            return;

        if (!player.TryTakeCargo(cargoData))
            return;

        SpawnedWorldObject spawnedObject =
            GetComponent<SpawnedWorldObject>();

        spawnedObject.Spawner.ReturnSpawnedObject(
            spawnedObject);
    }
}
