using DG.Tweening;
using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;

    private readonly Color[] addColors = new Color[]
    {
        new Color(1f, 1f, 1f),
        new Color(0.4f, 1f, 0.4f),
        new Color(0.2f, 0.8f, 0.2f)
    };

    private readonly Color[] minusColors = new Color[]
    {
        new Color(1f, 1f, 1f),
        new Color(1f, 0.5f, 0.5f),
        new Color(1f, 0.2f, 0.2f)
    };

    public void Init(ScoreSystem scoreSystem)
    {
        scoreSystem.OnScoreChanged += UpdateScore;
    }

    private void UpdateScore(int score, int delta)
    {
        scoreText.text = score.ToString();

        if (delta > 0)
        {
            AnimateScale(1.2f);
            AnimateColor(addColors);
        }
        else if (delta < 0)
        {
            AnimateScale(0.8f);
            AnimateColor(minusColors);
        }
    }

    private void AnimateScale(float targetScale)
    {
        scoreText.transform.DOKill();
        scoreText.transform.localScale = Vector3.one;
        scoreText.transform
            .DOScale(targetScale, 0.15f)
            .SetLoops(2, LoopType.Yoyo);
    }

    private void AnimateColor(Color[] colors)
    {
        scoreText.DOColor(colors[1], 0.15f)
            .OnComplete(() =>
            {
                scoreText.DOColor(colors[2], 0.15f)
                    .OnComplete(() =>
                    {
                        scoreText.DOColor(colors[0], 0.15f);
                    });
            });
    }
}