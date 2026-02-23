using UnityEngine;
using UnityEngine.UI;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private Text scoreText;

    private int score;

    public void AddScore(int amount)
    {
        score += amount;
        scoreText.text = score.ToString();
    }
}
