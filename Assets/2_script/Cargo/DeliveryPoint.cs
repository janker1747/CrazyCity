using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DeliveryPoint : MonoBehaviour
{
    private Player targetPlayer;
    private Collider triggerCollider;

    public void Init(Player player)
    {
        targetPlayer = player;
    }

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (targetPlayer == null)
            return;

        Player enteredPlayer = other.GetComponentInParent<Player>();
        if (enteredPlayer == null || enteredPlayer != targetPlayer)
            return;

        if (!targetPlayer.HasActiveCargo)
            return;

        targetPlayer.CompleteDelivery(true);
    }
}
