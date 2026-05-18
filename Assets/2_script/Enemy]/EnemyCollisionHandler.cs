using UnityEngine;

public class EnemyCollisionHandler : MonoBehaviour
{
    [SerializeField] private int _damage = 1;

    private void OnCollisionEnter(Collision collision)
    {
        Player player = collision.gameObject.GetComponent<Player>();
        PlayerCargoModule cargoModule = collision.gameObject.GetComponent<PlayerCargoModule>();
            
        if (player == null)
            return;

        if (player.HasShield)
        {
            player.ConsumeShield();
            Debug.Log("Shield absorbed the hit");
            return;
        }

        int damage = player.ModifyCargoScoreDamage((int)_damage);
        if (damage <= 0)
            return;
    
        player.RemoveScore(damage);
        cargoModule.TakeDamage(_damage);
        player.NotifyCargoScoreDamage(damage);
    }
}
