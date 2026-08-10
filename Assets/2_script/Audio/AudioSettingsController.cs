using UnityEngine;
using UnityEngine.UI;

public sealed class AudioSettingsController : MonoBehaviour
{
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider soundsVolumeSlider;

    private void Awake()
    {
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.SetValueWithoutNotify(GameAudio.MusicVolume);
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (soundsVolumeSlider != null)
        {
            soundsVolumeSlider.SetValueWithoutNotify(GameAudio.SfxVolume);
            soundsVolumeSlider.onValueChanged.AddListener(SetSoundsVolume);
        }
    }

    private void OnDestroy()
    {
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);

        if (soundsVolumeSlider != null)
            soundsVolumeSlider.onValueChanged.RemoveListener(SetSoundsVolume);
    }

    public void SetMusicVolume(float value)
    {
        GameAudio.SetMusicVolume(value);
    }

    public void SetSoundsVolume(float value)
    {
        GameAudio.SetSfxVolume(value);
    }
}
