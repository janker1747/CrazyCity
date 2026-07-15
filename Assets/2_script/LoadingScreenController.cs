using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreenController : MonoBehaviour
{
    [Header("Side Cover")]
    [SerializeField] private Image _leftCover;
    [SerializeField] private Image _rightCover;

    [Header("Loading UI")]
    [SerializeField] private GameObject _progressRoot;
    [SerializeField] private Image _progressFill;
    [SerializeField] private TMP_Text _progressText;

    [Header("3D Loading Scene")]
    [SerializeField] private GameObject _loading3DRoot;
    [SerializeField] private Transform _loading3DVisual;

    [Header("Animation")]
    [SerializeField, Min(0.1f)] private float _coverDuration = 0.65f;
    [SerializeField, Min(0.1f)] private float _uncoverDuration = 0.65f;
    [SerializeField, Min(0f)] private float _minimumLoadingDuration = 1.5f;
    [SerializeField, Min(0.1f)] private float _progressSpeed = 1.5f;

    private bool _isLoading;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        InitializeVisuals();
    }

    private void InitializeVisuals()
    {
        _leftCover.fillAmount = 0f;
        _rightCover.fillAmount = 0f;

        _progressRoot.SetActive(false);
        _loading3DRoot.SetActive(false);

        SetProgress(0f);
    }

    public void LoadScene(string sceneName)
    {
        if (_isLoading)
            return;

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("LoadingScreenController: scene name is empty.");
            return;
        }

        _isLoading = true;

        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        // Работает даже если Time.timeScale равен нулю.
        Sequence coverSequence = DOTween.Sequence()
            .SetUpdate(true);

        coverSequence.Join(
            _leftCover
                .DOFillAmount(1f, _coverDuration)
                .SetEase(Ease.InOutCubic)
        );

        coverSequence.Join(
            _rightCover
                .DOFillAmount(1f, _coverDuration)
                .SetEase(Ease.InOutCubic)
        );

        yield return coverSequence.WaitForCompletion();

        ShowLoadingContent();

        AsyncOperation loadingOperation = SceneManager.LoadSceneAsync(
            sceneName,
            LoadSceneMode.Single
        );

        if (loadingOperation == null)
        {
            Debug.LogError(
                $"LoadingScreenController: failed to load scene '{sceneName}'. " +
                "Check Build Settings."
            );

            _isLoading = false;
            yield break;
        }

        // Не активируем новую сцену, пока полоска визуально не дошла до конца.
        loadingOperation.allowSceneActivation = false;

        float displayedProgress = 0f;
        float loadingTime = 0f;

        while (true)
        {
            loadingTime += Time.unscaledDeltaTime;

            // Unity загружает сцену от 0 до 0.9.
            float targetProgress = Mathf.Clamp01(
                loadingOperation.progress / 0.9f
            );

            displayedProgress = Mathf.MoveTowards(
                displayedProgress,
                targetProgress,
                _progressSpeed * Time.unscaledDeltaTime
            );

            SetProgress(displayedProgress);

            bool sceneLoaded = loadingOperation.progress >= 0.9f;
            bool progressFinished = displayedProgress >= 0.999f;
            bool minimumTimePassed =
                loadingTime >= _minimumLoadingDuration;

            if (sceneLoaded &&
                progressFinished &&
                minimumTimePassed)
            {
                break;
            }

            yield return null;
        }

        SetProgress(1f);

        // Разрешаем Unity переключиться на игровую сцену.
        loadingOperation.allowSceneActivation = true;

        while (!loadingOperation.isDone)
            yield return null;

        // Сцена выбора уже выгружена.
        // Возвращаем нормальное течение времени на случай,
        // если экран был вызван после смерти и Time.timeScale был 0.
        Time.timeScale = 1f;

        // Даём новой сцене полностью отрисовать первый кадр.
        yield return null;
        yield return null;

        yield return HideLoadingScreen();

        Destroy(gameObject);
    }

    private void ShowLoadingContent()
    {
        _progressRoot.SetActive(true);
        _loading3DRoot.SetActive(true);

        if (_loading3DVisual != null)
        {
            _loading3DVisual.localScale = Vector3.zero;

            _loading3DVisual
                .DOScale(Vector3.one, 0.35f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }
    }

    private IEnumerator HideLoadingScreen()
    {
        Sequence hideContentSequence = DOTween.Sequence()
            .SetUpdate(true);

        if (_loading3DVisual != null)
        {
            hideContentSequence.Join(
                _loading3DVisual
                    .DOScale(Vector3.zero, 0.25f)
                    .SetEase(Ease.InBack)
            );
        }

        yield return hideContentSequence.WaitForCompletion();

        _progressRoot.SetActive(false);
        _loading3DRoot.SetActive(false);

        Sequence uncoverSequence = DOTween.Sequence()
            .SetUpdate(true);

        uncoverSequence.Join(
            _leftCover
                .DOFillAmount(0f, _uncoverDuration)
                .SetEase(Ease.InOutCubic)
        );

        uncoverSequence.Join(
            _rightCover
                .DOFillAmount(0f, _uncoverDuration)
                .SetEase(Ease.InOutCubic)
        );

        yield return uncoverSequence.WaitForCompletion();
    }

    private void SetProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);

        if (_progressFill != null)
            _progressFill.fillAmount = progress;

        if (_progressText != null)
            _progressText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
    }

    private void OnDestroy()
    {
        DOTween.Kill(_leftCover);
        DOTween.Kill(_rightCover);

        if (_loading3DVisual != null)
            DOTween.Kill(_loading3DVisual);
    }
}