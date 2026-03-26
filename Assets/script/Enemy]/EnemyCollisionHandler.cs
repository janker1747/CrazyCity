using UnityEngine;

public class EnemyCollisionHandler : MonoBehaviour
{
    [SerializeField] private float _damage = 1f;

    private void OnCollisionEnter(Collision collision)
    {
        Player player = collision.gameObject.GetComponent<Player>();

        if (player == null)
            return;

        if (player.HasShield)
        {
            player.ConsumeShield();
            Debug.Log("Shield absorbed the hit");
            return;
        }

        player.RemoveScore((int)_damage);
    }
}