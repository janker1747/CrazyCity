using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCollisionHandler : MonoBehaviour
{
    [SerializeField] private float _damage = 1f;

    private void OnCollisionEnter(Collision collision)
    {
        Player player = collision.gameObject.GetComponent<Player>();

        if (player != null)
        {
            player.gameObject.GetComponent<PlayerHealth>().TakeDamage(_damage);
        }
    }
}
