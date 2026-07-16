using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class ChoisePlayerUI : MonoBehaviour
{
    [SerializeField] private LoadingSceneCanvas _loadingSceneCanvas;
    [SerializeField] private float _fadeDuration = 0.3f;

    private CanvasGroup _loadingCanvasGroup;
    
    private void Awake()
    {
        _loadingCanvasGroup = _loadingSceneCanvas.GetComponent<CanvasGroup>();

        _loadingCanvasGroup.alpha = 0f;
        _loadingCanvasGroup.interactable = false;
        _loadingCanvasGroup.blocksRaycasts = false;
    }

    public void StartGame()
    {
        CarSelectionManager.Instance?.ConfirmSelection();

        _loadingCanvasGroup.DOKill();

        _loadingCanvasGroup.interactable = true;
        _loadingCanvasGroup.blocksRaycasts = true;

        _loadingCanvasGroup
            .DOFade(1f, _fadeDuration)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _loadingSceneCanvas.LoadScene();
            });
    }
}
