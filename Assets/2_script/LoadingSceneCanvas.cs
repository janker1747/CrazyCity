using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingSceneCanvas : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string _sceneName;

    [Header("UI")]
    [SerializeField] private Slider _loadingSlider;
    [SerializeField] private TMP_Text _loadingText;

    [Header("Settings")]
    [SerializeField] private string _loadingMessage = "Loading...";
    [SerializeField, Min(0f)] private float _minimumLoadingTime = 0.5f;

    private bool _isLoading;

    private void Awake()
    {
        if (_loadingSlider != null)
        {
            _loadingSlider.minValue = 0f;
            _loadingSlider.maxValue = 10f;
            _loadingSlider.value = 0f;
        }

        SetProgress(0f);
    }

    
    public void LoadScene()
    {
        if (_isLoading)
            return;

        if (string.IsNullOrWhiteSpace(_sceneName))
        {
            Debug.LogError(
                $"{nameof(LoadingSceneCanvas)}: имя сцены не указано.",
                this);

            return;
        }

        StartCoroutine(LoadSceneRoutine());
    }

    private IEnumerator LoadSceneRoutine()
    {
        _isLoading = true;
        SetProgress(0f);

        AsyncOperation operation = SceneManager.LoadSceneAsync(
            _sceneName,
            LoadSceneMode.Single);

        if (operation == null)
        {
            Debug.LogError(
                $"{nameof(LoadingSceneCanvas)}: не удалось начать загрузку сцены '{_sceneName}'.",
                this);

            _isLoading = false;
            yield break;
        }

        operation.allowSceneActivation = false;

        float elapsedTime = 0f;

        while (operation.progress < 0.9f)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float normalizedProgress =
                Mathf.Clamp01(operation.progress / 0.9f);

            SetProgress(normalizedProgress);

            yield return null;
        }

        SetProgress(1f);

        while (elapsedTime < _minimumLoadingTime)
        {
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        operation.allowSceneActivation = true;
    }

    private void SetProgress(float normalizedProgress)
    {
        normalizedProgress = Mathf.Clamp01(normalizedProgress);

        float sliderValue = normalizedProgress * 10f;

        if (_loadingSlider != null)
            _loadingSlider.value = sliderValue;

        if (_loadingText != null)
        {
            _loadingText.text =
                $"{_loadingMessage} {Mathf.RoundToInt(sliderValue)}/10";
        }
    }
}