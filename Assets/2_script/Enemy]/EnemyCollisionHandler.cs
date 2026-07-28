using UnityEngine;
using System;
using System.Collections;

public class EnemyCollisionHandler : MonoBehaviour
{
    [SerializeField] private int _damage = 1;
    [SerializeField] private int _ScoreDamage = 100;
    [SerializeField] private float _damageCooldown = 8f;
    
    private bool _canDealDamage = true;
    private Coroutine _damageCooldownCoroutine;
    private int _lastProcessedFrame = -1; // Защита от множественных вызовов в одном кадре

    public event Action OnCollidedWithPlayer; 

    private void OnCollisionEnter(Collision collision)
    {
        if (!_canDealDamage)
            return;

        if (_lastProcessedFrame == Time.frameCount)
            return;
            
        _lastProcessedFrame = Time.frameCount;

        _canDealDamage = false;
            
        Player player = collision.gameObject.GetComponentInParent<Player>();
            
        if (player == null)
        {
            _canDealDamage = true; // Разблокируем если это не игрок
            return;
        }

        OnCollidedWithPlayer?.Invoke();

        if (player.TryConsumeShield())
        {
            StartDamageCooldown();
            return;
        }

        PlayerCargoModule cargoModule = player.CargoModule;

        int damage = player.ModifyCargoScoreDamage(_damage);
        
        if (damage <= 0)
        {
            StartDamageCooldown();
            return;
        }
        
        bool inTakeDamage = cargoModule != null && cargoModule.TakeDamage(damage);

        if (inTakeDamage == false)
        { 
            player.TakeDamage(damage);
        }
        
        player.RemoveScore(_ScoreDamage);
        player.NotifyCargoScoreDamage(damage);
        Debug.Log($"Dealing {damage} damage. Player health before: {player.Health.CurrentHealth}");
        StartDamageCooldown();
    }

    private void StartDamageCooldown()
    {
        if (_damageCooldownCoroutine != null)
            StopCoroutine(_damageCooldownCoroutine);
            
        _damageCooldownCoroutine = StartCoroutine(DamageCooldownRoutine());
    }

    private IEnumerator DamageCooldownRoutine()
    {
        yield return new WaitForSeconds(_damageCooldown);
        _canDealDamage = true;
        _lastProcessedFrame = -1;
    }

    private void OnCollisionStay(Collision collision)
    {
        // Если кулдаун прошел и враг всё еще касается игрока - не наносим урон автоматически
        // Можно добавить логику если нужно, но обычно достаточно кулдауна
    }
}
