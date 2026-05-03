using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DeliveryPoint : MonoBehaviour
{
    private Player targetPlayer;
    private Collider triggerCollider;

    public void Init(Player player)
    {
        targetPlayer = player;

        if (targetPlayer == null)
            Debug.LogWarning($"{nameof(DeliveryPoint)} on {name}: initialized without a player.");
    }

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();

        if (triggerCollider == null)
        {
            Debug.LogWarning($"{nameof(DeliveryPoint)} on {name}: Collider is missing.");
            return;
        }

        if (!triggerCollider.isTrigger)
        {
            Debug.LogWarning($"{nameof(DeliveryPoint)} on {name}: Collider must be a trigger. It was enabled automatically.");
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (targetPlayer == null)
            return;

        Player enteredPlayer = other.GetComponentInParent<Player>();
        if (enteredPlayer == null || enteredPlayer != targetPlayer)
            return;

        if (targetPlayer.CurrentCargo == null)
            return;

        targetPlayer.CompleteDelivery(true);
        Destroy(gameObject);
    }
}
