using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CargoPickup : MonoBehaviour
{
    private static readonly List<CargoPickup> activePickups = new();

    [SerializeField] private Cargo cargoData;
    [SerializeField] private bool destroyOnPickup = true;

    private Collider pickupCollider;

    public static IReadOnlyList<CargoPickup> ActivePickups => activePickups;
    public Cargo CargoData => cargoData;
    public bool IsAvailable => isActiveAndEnabled && cargoData != null;

    private void Awake()
    {
        pickupCollider = GetComponent<Collider>();

        if (pickupCollider == null)
        {
            Debug.LogWarning($"{nameof(CargoPickup)} on {name}: Collider is missing.");
            return;
        }

        if (!pickupCollider.isTrigger)
        {
            Debug.LogWarning($"{nameof(CargoPickup)} on {name}: Collider must be a trigger. It was enabled automatically.");
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
        if (cargoData == null)
        {
            Debug.LogWarning($"{nameof(CargoPickup)} on {name}: cargo data is not assigned.");
            return;
        }

        Player player = other.GetComponentInParent<Player>();
        if (player == null)
            return;

        if (!player.TryTakeCargo(cargoData))
            return;

        if (destroyOnPickup)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }
}
