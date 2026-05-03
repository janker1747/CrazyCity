using UnityEngine;

[CreateAssetMenu(menuName = "Game/Impact Data")]
public class ImpactData : ScriptableObject
{
    [Header("Particles")]
    public ParticleSystem particlePrefab;

    [Header("Sound")]
    public AudioClip sound;

    [Header("Score")]
    public int score;

    [Header("Camera")]
    public float cameraShake;

    [Header ("Rock")]
    public GameObject rockPrefab;
}