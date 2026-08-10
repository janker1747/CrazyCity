using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public static class GameAudio
{
    public const string MusicVolumeParameter = "MusicVolume";
    public const string SfxVolumeParameter = "SfxVolume";

    private const string ConfigResourceName = "GameAudioConfig";
    private const string MusicVolumeKey = "Audio.MusicVolume";
    private const string SfxVolumeKey = "Audio.SfxVolume";

    private static GameAudioConfig config;
    private static GameAudioHost host;
    private static bool initialized;

    public static float MusicVolume => PlayerPrefs.GetFloat(MusicVolumeKey, 0.8f);
    public static float SfxVolume => PlayerPrefs.GetFloat(SfxVolumeKey, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        initialized = false;
        config = null;
        host = null;
        EnsureInitialized();
    }

    public static void PlaySfx(GameAudioCue cue)
    {
        EnsureInitialized();
        host?.Play2D(config.GetClip(cue));
    }

    public static void PlaySfx(GameAudioCue cue, Vector3 position)
    {
        EnsureInitialized();
        host?.PlayAtPosition(config.GetClip(cue), position);
    }

    public static void StartLoop(GameAudioCue cue, Vector3 position)
    {
        EnsureInitialized();
        host?.StartLoop(config.GetClip(cue), position);
    }

    public static void StopLoop()
    {
        EnsureInitialized();
        host?.StopLoop();
    }

    public static void SetMusicVolume(float normalizedVolume)
    {
        SetVolume(MusicVolumeKey, MusicVolumeParameter, normalizedVolume);
    }

    public static void SetSfxVolume(float normalizedVolume)
    {
        SetVolume(SfxVolumeKey, SfxVolumeParameter, normalizedVolume);
    }

    private static void SetVolume(string playerPrefsKey, string mixerParameter, float value)
    {
        EnsureInitialized();

        float normalizedVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(playerPrefsKey, normalizedVolume);

        if (config != null && config.Mixer != null)
        {
            float maxDecibels = mixerParameter == MusicVolumeParameter
                ? config.MusicMaxVolumeDecibels
                : 0f;

            config.Mixer.SetFloat(
                mixerParameter,
                ToDecibels(normalizedVolume, maxDecibels));
        }
    }

    private static float ToDecibels(float normalizedVolume, float maxDecibels = 0f)
    {
        return normalizedVolume <= 0.0001f
            ? -80f
            : Mathf.Log10(normalizedVolume) * 20f + maxDecibels;
    }

    private static void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;
        config = Resources.Load<GameAudioConfig>(ConfigResourceName);

        if (config == null)
        {
            Debug.LogError($"Resources/{ConfigResourceName} is missing.");
            return;
        }

        GameObject hostObject = new GameObject("GameAudio");
        Object.DontDestroyOnLoad(hostObject);
        host = hostObject.AddComponent<GameAudioHost>();
        host.Initialize(config);

        config.Mixer?.SetFloat(
            MusicVolumeParameter,
            ToDecibels(MusicVolume, config.MusicMaxVolumeDecibels));
        config.Mixer?.SetFloat(SfxVolumeParameter, ToDecibels(SfxVolume));
    }
}

internal sealed class GameAudioHost : MonoBehaviour
{
    private GameAudioConfig config;
    private AudioSource firstMusicSource;
    private AudioSource secondMusicSource;
    private AudioSource currentMusicSource;
    private AudioSource sfxSource;
    private AudioSource loopSource;
    private Coroutine musicTransition;

    public void Initialize(GameAudioConfig audioConfig)
    {
        config = audioConfig;
        firstMusicSource = CreateSource("Music A", config.MusicGroup, 0f, true);
        secondMusicSource = CreateSource("Music B", config.MusicGroup, 0f, true);
        currentMusicSource = firstMusicSource;
        firstMusicSource.volume = 0f;
        secondMusicSource.volume = 0f;
        sfxSource = CreateSource("SFX 2D", config.SfxGroup, 0f, false);
        loopSource = CreateSource("SFX Loop", config.SfxGroup, 1f, true);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            PlayerPrefs.Save();
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }

    public void Play2D(AudioClip clip)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip);
    }

    public void PlayAtPosition(AudioClip clip, Vector3 position)
    {
        if (clip == null)
            return;

        GameObject soundObject = new GameObject($"SFX {clip.name}");
        soundObject.transform.SetParent(transform);
        soundObject.transform.position = position;

        AudioSource source = soundObject.AddComponent<AudioSource>();
        ConfigureSource(source, config.SfxGroup, 1f, false);
        source.clip = clip;
        source.Play();
        Destroy(soundObject, clip.length + 0.1f);
    }

    public void StartLoop(AudioClip clip, Vector3 position)
    {
        if (clip == null)
            return;

        loopSource.Stop();
        loopSource.transform.position = position;
        loopSource.clip = clip;
        loopSource.Play();
    }

    public void StopLoop()
    {
        loopSource.Stop();
        loopSource.clip = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RegisterButtonSounds(scene);

        if (scene.name == "MainGame")
            CrossfadeMusic(
                config.GetRandomGameplayMusic(),
                config.GameplayTransitionDuration,
                1f);
        else if (scene.name == "MainMenu" ||
                 scene.name == "ChoiseCar3D" ||
                 scene.name == "GameOverScene")
            CrossfadeMusic(config.MenuMusic, 1.5f, config.MenuMusicPitch);
    }

    private void CrossfadeMusic(AudioClip clip, float duration, float pitch)
    {
        if (clip == null ||
            currentMusicSource.clip == clip && currentMusicSource.isPlaying)
            return;

        if (musicTransition != null)
            StopCoroutine(musicTransition);

        AudioSource outgoingSource = currentMusicSource;
        AudioSource incomingSource = currentMusicSource == firstMusicSource
            ? secondMusicSource
            : firstMusicSource;

        incomingSource.Stop();
        incomingSource.clip = clip;
        incomingSource.volume = 0f;
        incomingSource.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
        incomingSource.Play();
        currentMusicSource = incomingSource;

        musicTransition = StartCoroutine(
            CrossfadeMusicRoutine(outgoingSource, incomingSource, duration));
    }

    private IEnumerator CrossfadeMusicRoutine(
        AudioSource outgoingSource,
        AudioSource incomingSource,
        float duration)
    {
        float outgoingStartVolume = outgoingSource.isPlaying
            ? outgoingSource.volume
            : 0f;
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = safeDuration <= 0f
                ? 1f
                : Mathf.Clamp01(elapsed / safeDuration);
            progress = Mathf.SmoothStep(0f, 1f, progress);

            outgoingSource.volume = outgoingStartVolume * (1f - progress);
            incomingSource.volume = progress;
            yield return null;
        }

        outgoingSource.Stop();
        outgoingSource.clip = null;
        outgoingSource.volume = 0f;
        incomingSource.volume = 1f;
        musicTransition = null;
    }

    private void RegisterButtonSounds(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.GetComponent<UIButtonAudio>() == null)
                    button.gameObject.AddComponent<UIButtonAudio>();
            }
        }
    }

    private AudioSource CreateSource(
        string sourceName,
        AudioMixerGroup group,
        float spatialBlend,
        bool loop)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform);
        AudioSource source = sourceObject.AddComponent<AudioSource>();
        ConfigureSource(source, group, spatialBlend, loop);
        return source;
    }

    private static void ConfigureSource(
        AudioSource source,
        AudioMixerGroup group,
        float spatialBlend,
        bool loop)
    {
        source.playOnAwake = false;
        source.outputAudioMixerGroup = group;
        source.spatialBlend = spatialBlend;
        source.loop = loop;
        source.minDistance = 2f;
        source.maxDistance = 35f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
    }
}

[DisallowMultipleComponent]
internal sealed class UIButtonAudio : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button?.onClick.AddListener(PlayClick);
    }

    private void OnDestroy()
    {
        button?.onClick.RemoveListener(PlayClick);
    }

    private void PlayClick()
    {
        GameAudio.PlaySfx(GameAudioCue.ButtonClick);
    }
}
