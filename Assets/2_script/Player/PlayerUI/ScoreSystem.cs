using System;
using UnityEngine;

public class ScoreSystem
{
    public event Action<int, int> OnScoreChanged;

    private int _score;
    private int _minScore;

    private float _multiplier = 1f;
    private float _contractMultiplier;
    private bool _safeActive = false;

    public int Score => _score;

    public event Action<int> OnScoreAdded;
    public event Action<int> OnScoreRemoved;

    public void AddScore(int amount)
    {
        int final = Mathf.RoundToInt(amount * _multiplier * GetTotalMultiplier());

        _score += final;

        OnScoreChanged?.Invoke(_score, final);
        OnScoreAdded?.Invoke(final);

        if (final > 0)
            GameAudio.PlaySfx(GameAudioCue.ScoreGain);
    }

    public void MinusScore(int amount)
    {
        int before = _score;

        _score -= amount;

        if (_safeActive && _score < _minScore)
        {
            _score = _minScore;
        }

        int delta = _score - before;

        OnScoreChanged?.Invoke(_score, delta);

        if (delta < 0)
        {
            OnScoreRemoved?.Invoke(-delta);
            GameAudio.PlaySfx(GameAudioCue.ScoreLoss);
        }
    }

    public void SetMultiplier(float value)
    {
        _multiplier = value;
    }

    public void AddContractMultiplier(float value)
    {
        _contractMultiplier += Mathf.Max(0f, value);
    }

    private float GetTotalMultiplier()
    {
        return _contractMultiplier > 0f ? _contractMultiplier : 1f;
    }

    public void ActivateSafe()
    {
        _safeActive = true;
        _minScore = _score;
    }

    public void DeactivateSafe()
    {
        _safeActive = false;
    }
}
