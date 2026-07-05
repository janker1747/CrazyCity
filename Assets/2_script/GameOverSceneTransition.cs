using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverSceneTransition : MonoBehaviour
{
    private const string DefaultSceneName = "GameOverScene";

    public static GameOverSceneTransition Instance { get; private set; }

    [SerializeField] private string _gameOverSceneName = DefaultSceneName;
    [SerializeField] private CanvasGroup _fadeCanvasGroup;
    [SerializeField] private float _fadeDuration = 0.8f;

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
        Time.timeScale = 1f;

        CanvasGroup fadeCanvasGroup = GetOrCreateFadeCanvasGroup();
        fadeCanvasGroup.gameObject.SetActive(true);
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = true;

        Tween fadeTween = fadeCanvasGroup
            .DOFade(1f, Mathf.Max(0f, _fadeDuration))
            .SetUpdate(true);

        yield return fadeTween.WaitForCompletion();

        SceneManager.LoadScene(sceneName);

        if (_destroyAfterLoad)
        {
            if (_runtimeFadeCanvasObject != null)
                Destroy(_runtimeFadeCanvasObject);

            Destroy(gameObject);
        }
    }

    private CanvasGroup GetOrCreateFadeCanvasGroup()
    {
        if (_fadeCanvasGroup != null)
            return _fadeCanvasGroup;

        Canvas canvas = new GameObject(
            "Game Over Fade Canvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)).GetComponent<Canvas>();

        _runtimeFadeCanvasObject = canvas.gameObject;
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
