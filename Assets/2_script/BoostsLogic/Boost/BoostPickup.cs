using UnityEngine;
using _2_script;

class BoostPickup : MonoBehaviour
{
    [SerializeField] private BoostData data;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out Player player))
            return;

        player.BoostSlot.Set(data);
        GameAudio.PlaySfx(GameAudioCue.PickupBoost, transform.position);
        
        SpawnedWorldObject spawnedObject =
            GetComponent<SpawnedWorldObject>();

        spawnedObject.Spawner.ReturnSpawnedObject(
            spawnedObject);
    }
}
