using UnityEngine;

public class ImpactSystem : MonoBehaviour
{
    [SerializeField] private PlayerCollisionHandler _collisionHandler;
    [SerializeField] private ParticlePool _particlePool;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private Player _player;

    private ScoreSystem _scoreSystem;

    private void Start()
    {
        _scoreSystem = _player.ScoreSystem;
    }

    private void OnEnable()
    {
        _collisionHandler.OnImpact += HandleImpact;
    }

    private void OnDisable()
    {
        _collisionHandler.OnImpact -= HandleImpact;
    }

    private void HandleImpact(Vector3 position, ImpactData data)
    {
        PlayParticles(position, data);
        PlaySound(data);
        AddScore(data);
        ShakeCamera(data);
    }

    private void PlayParticles(Vector3 position, ImpactData data)
    {
        if (data.particlePrefab == null)
            return;

        ParticleSystem particle = _particlePool.GetParticle(data.particlePrefab);

        particle.transform.position = position;
        particle.Play();
    }

    private void PlaySound(ImpactData data)
    {
        if (data.sound == null)
            return;

        _audioSource.PlayOneShot(data.sound);
    }

    private void AddScore(ImpactData data)
    {
        if (data.score == 0)
            return;

        if (data.score > 0)
            _scoreSystem.AddScore(data.score);
        else
            _scoreSystem.MinusScore(-data.score);
    }

    private void ShakeCamera(ImpactData data)
    {
        if (data.cameraShake <= 0)
            return;
    }
}