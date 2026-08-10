using UnityEngine;
using UnityEngine.Audio;

public enum GameAudioCue
{
    ChargeStart,
    ChargeLoop,
    ChargeComplete,
    ChargeCancel,
    PickupCargo,
    PickupBoost,
    DeliverySuccess,
    BoostActivate,
    ButtonClick,
    BombExplosion,
    PlayerDamage,
    ScoreGain,
    ScoreLoss,
    CollisionImpact
}

[CreateAssetMenu(menuName = "Game/Audio Config")]
public sealed class GameAudioConfig : ScriptableObject
{
    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("Music")]
    [SerializeField] private AudioClip[] gameplayMusic;
    [SerializeField] private AudioClip menuMusic;
    [SerializeField, Min(1f)] private float gameplayTransitionDuration = 15f;
    [SerializeField, Range(0.75f, 1f)] private float menuMusicPitch = 0.92f;
    [SerializeField, Range(-30f, 0f)] private float musicMaxVolumeDecibels = -10f;

    [Header("Charge Zone")]
    [SerializeField] private AudioClip chargeStart;
    [SerializeField] private AudioClip chargeLoop;
    [SerializeField] private AudioClip chargeComplete;
    [SerializeField] private AudioClip chargeCancel;

    [Header("Gameplay")]
    [SerializeField] private AudioClip pickupCargo;
    [SerializeField] private AudioClip pickupBoost;
    [SerializeField] private AudioClip deliverySuccess;
    [SerializeField] private AudioClip boostActivate;
    [SerializeField] private AudioClip bombExplosion;
    [SerializeField] private AudioClip playerDamage;
    [SerializeField] private AudioClip collisionImpact;

    [Header("UI and Score")]
    [SerializeField] private AudioClip buttonClick;
    [SerializeField] private AudioClip scoreGain;
    [SerializeField] private AudioClip scoreLoss;

    public AudioMixer Mixer => mixer;
    public AudioMixerGroup MusicGroup => musicGroup;
    public AudioMixerGroup SfxGroup => sfxGroup;
    public AudioClip MenuMusic => menuMusic;
    public float GameplayTransitionDuration => gameplayTransitionDuration;
    public float MenuMusicPitch => menuMusicPitch;
    public float MusicMaxVolumeDecibels => musicMaxVolumeDecibels;

    public AudioClip GetRandomGameplayMusic()
    {
        if (gameplayMusic == null || gameplayMusic.Length == 0)
            return null;

        return gameplayMusic[Random.Range(0, gameplayMusic.Length)];
    }

    public AudioClip GetClip(GameAudioCue cue)
    {
        switch (cue)
        {
            case GameAudioCue.ChargeStart: return chargeStart;
            case GameAudioCue.ChargeLoop: return chargeLoop;
            case GameAudioCue.ChargeComplete: return chargeComplete;
            case GameAudioCue.ChargeCancel: return chargeCancel;
            case GameAudioCue.PickupCargo: return pickupCargo;
            case GameAudioCue.PickupBoost: return pickupBoost;
            case GameAudioCue.DeliverySuccess: return deliverySuccess;
            case GameAudioCue.BoostActivate: return boostActivate;
            case GameAudioCue.ButtonClick: return buttonClick;
            case GameAudioCue.BombExplosion: return bombExplosion;
            case GameAudioCue.PlayerDamage: return playerDamage;
            case GameAudioCue.ScoreGain: return scoreGain;
            case GameAudioCue.ScoreLoss: return scoreLoss;
            case GameAudioCue.CollisionImpact: return collisionImpact;
            default: return null;
        }
    }
}
