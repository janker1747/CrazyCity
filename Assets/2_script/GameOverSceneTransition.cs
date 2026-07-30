using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverSceneTransition : MonoBehaviour
{
    private const string DefaultSceneName = "GameOverScene";

    public static GameOverSceneTransition Instance { get; private set; }

    [SerializeField] private string _gameOverSceneName = DefaultSceneName;
    [SerializeField] private CanvasGroup _fadeCanvasGroup;
    [SerializeField, Min(0f)] private float _slowMotionDuration = 1.2f;
    [SerializeField, Range(0.01f, 1f)] private float _deathTimeScale = 0.1f;
    [SerializeField, Min(0f)] private float _fadeDelay = 0.15f;
    [SerializeField, Min(0f)] private float _fadeDuration = 1.05f;

    private bool _isLoading;
    private bool _destroyAfterLoad;
    private GameObject _runtimeFadeCanvasObject;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (_isLoading)
            Time.timeScale = 1f;

        if (Instance == this)
            Instance = null;
    }

    public static void LoadGameOver()
    {
        if (Instance == null)
            Instance = CreateRuntimeTransition();

        Instance.LoadScene(Instance._gameOverSceneName);
    }

    public void LoadScene(string sceneName)
    {
        if (_isLoading)
            return;

        StartCoroutine(LoadSceneRoutine(string.IsNullOrWhiteSpace(sceneName) ? DefaultSceneName : sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        _isLoading = true;

        CanvasGroup fadeCanvasGroup = GetOrCreateFadeCanvasGroup();
        fadeCanvasGroup.gameObject.SetActive(true);
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = true;

        float startTimeScale = Mathf.Clamp(Time.timeScale, 0.01f, 1f);
        float targetTimeScale = Mathf.Min(startTimeScale, _deathTimeScale);
        float transitionDuration = Mathf.Max(
            _slowMotionDuration,
            _fadeDelay + _fadeDuration);
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float slowMotionProgress = _slowMotionDuration <= 0f
                ? 1f
                : Mathf.Clamp01(elapsed / _slowMotionDuration);
            slowMotionProgress = Mathf.SmoothStep(0f, 1f, slowMotionProgress);
            Time.timeScale = Mathf.Lerp(startTimeScale, targetTimeScale, slowMotionProgress);

            float fadeProgress = _fadeDuration <= 0f
                ? (elapsed >= _fadeDelay ? 1f : 0f)
                : Mathf.Clamp01((elapsed - _fadeDelay) / _fadeDuration);
            fadeCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, fadeProgress);

            yield return null;
        }

        Time.timeScale = targetTimeScale;
        fadeCanvasGroup.alpha = 1f;

        // Даём полностью чёрному кадру отрисоваться перед загрузкой.
        yield return null;

        Time.timeScale = 1f;
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);

        if (loadOperation == null)
        {
            Debug.LogError(
                $"{nameof(GameOverSceneTransition)}: не удалось загрузить сцену '{sceneName}'.",
                this);
            _isLoading = false;
            yield break;
        }

        while (!loadOperation.isDone)
            yield return null;

        if (_destroyAfterLoad)
        {
            if (_runtimeFadeCanvasObject != null)
                Destroy(_runtimeFadeCanvasObject);
            Destroy(gameObject);
        }
    }

    private CanvasGroup GetOrCreateFadeCanvasGroup()
    {
        if (_fadeCanvasGroup != null && _fadeCanvasGroup.TryGetComponent(out Image _))
            return _fadeCanvasGroup;

        Canvas canvas = new GameObject(
            "Game Over Fade Canvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)).GetComponent<Canvas>();

        _runtimeFadeCanvasObject = canvas.gameObject;
        if (_destroyAfterLoad)
            DontDestroyOnLoad(canvas.gameObject);

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        Image image = new GameObject(
            "Fade",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup)).GetComponent<Image>();

        image.transform.SetParent(canvas.transform, false);
        image.color = Color.black;

        RectTransform rectTransform = image.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        _fadeCanvasGroup = image.GetComponent<CanvasGroup>();
        return _fadeCanvasGroup;
    }

    private static GameOverSceneTransition CreateRuntimeTransition()
    {
        GameObject transitionObject = new GameObject(nameof(GameOverSceneTransition));
        DontDestroyOnLoad(transitionObject);
        GameOverSceneTransition transition = transitionObject.AddComponent<GameOverSceneTransition>();
        transition._destroyAfterLoad = true;
        return transition;
    }
}
