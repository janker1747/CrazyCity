using UnityEngine;

class BoostPickup : MonoBehaviour
{
    [SerializeField] private BoostData _data;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Player player))
        {
            player.BoostSlot.Set(_data);
            Destroy(gameObject);
        }
    }
}