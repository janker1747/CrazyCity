using UnityEngine;

public class ImpactSystem : MonoBehaviour
{
    public static ImpactSystem Current { get; private set; }

    [SerializeField] private PlayerCollisionHandler _collisionHandler;
    [SerializeField] private ParticlePool _particlePool;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private Player _player;

    private ScoreSystem _scoreSystem;

    private void Awake()
    {
        Current = this;
    }

    private void Start()
    {
        if (_player != null)
            _scoreSystem = _player.ScoreSystem;
    }

    private void OnEnable()
    {
        if (_collisionHandler != null)
            _collisionHandler.OnImpact += HandleImpact;
    }

    private void OnDisable()
    {
        if (_collisionHandler != null)
            _collisionHandler.OnImpact -= HandleImpact;

        if (Current == this)
            Current = null;
    }

    public void PlayFeedback(Vector3 position, ParticleSystem particlePrefab, AudioClip sound)
    {
        PlayParticles(position, particlePrefab);
        PlaySound(sound);
    }

    public void SetPlayer(Player player)
    {
        if (isActiveAndEnabled && _collisionHandler != null)
            _collisionHandler.OnImpact -= HandleImpact;

        _player = player;
        _collisionHandler = player != null ? player.PlayerCollision : null;
        _scoreSystem = player != null ? player.ScoreSystem : null;

        if (isActiveAndEnabled && _collisionHandler != null)
            _collisionHandler.OnImpact += HandleImpact;
    }

    private void HandleImpact(Vector3 position, ImpactData data)
    {
        if (data == null)
            return;

        PlayParticles(position, data);
        PlaySound(data);
        AddScore(data);
        ShakeCamera(data);
    }

    private void PlayParticles(Vector3 position, ImpactData data)
    {
        PlayParticles(position, data != null ? data.particlePrefab : null);
    }

    private void PlayParticles(Vector3 position, ParticleSystem particlePrefab)
    {
        if (particlePrefab == null || _particlePool == null)
            return;

        ParticleSystem particle = _particlePool.GetParticle(particlePrefab);

        particle.transform.position = position;
        particle.Play();
    }

    private void PlaySound(ImpactData data)
    {
        PlaySound(data != null ? data.sound : null);
    }

    private void PlaySound(AudioClip sound)
    {
        if (sound == null || _audioSource == null)
            return;

        _audioSource.PlayOneShot(sound);
    }

    private void AddScore(ImpactData data)
    {
        if (data == null || data.score == 0 || _scoreSystem == null)
            return;

        if (data.score > 0)
            _scoreSystem.AddScore(data.score);
        else
            _scoreSystem.MinusScore(-data.score);
    }

    private void ShakeCamera(ImpactData data)
    {
        if (data == null || data.cameraShake <= 0)
            return;
    }
}
