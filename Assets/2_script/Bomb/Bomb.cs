using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    [SerializeField] private BombTimer _timer;
    [SerializeField] private ParticleSystem _explosionFX;
    [SerializeField] private float _radius = 3f;
    [SerializeField] private int _damage = 10;

    private BombPool _pool;

    public void Initialize(BombPool pool)
    {
        _pool = pool;

        _timer.OnTimerCompleted -= Explode;
        _timer.OnTimerCompleted += Explode;

        _timer.StartTimer();
    }

    private void Explode()
    {
        _timer.OnTimerCompleted -= Explode;

        _explosionFX.Play();
        GameAudio.PlaySfx(GameAudioCue.BombExplosion, transform.position);

        Collider[] hits = Physics.OverlapSphere(transform.position, _radius);

        foreach (var hit in hits)
        {
            Player player = hit.GetComponent<Player>();
            if (player != null)
                player.TakeDamage(_damage);
        }

        StartCoroutine(ReturnAfterDelay());
    }

    private IEnumerator ReturnAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        _pool.ReturnObject(this);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}
