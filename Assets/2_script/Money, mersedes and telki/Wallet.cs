using System;
using UnityEngine;

public class Wallet
{
    private const string GoldSaveKey = "PLAYER_GOLD";

    private int _currentGold;

    public int CurrentGold => _currentGold;

    public event Action<int> GoldChanged;

    public Wallet()
    {
        Load();
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
            return;

        _currentGold += amount;

        Save();
        GoldChanged?.Invoke(_currentGold);
    }

    public bool TrySpendGold(int amount)
    {
        if (amount <= 0)
            return false;

        if (_currentGold < amount)
            return false;

        _currentGold -= amount;

        Save();
        GoldChanged?.Invoke(_currentGold);

        return true;
    }

    public void SetGold(int amount)
    {
        _currentGold = Mathf.Max(0, amount);

        Save();
        GoldChanged?.Invoke(_currentGold);
    }

    public void ResetGold()
    {
        _currentGold = 0;

        Save();
        GoldChanged?.Invoke(_currentGold);
    }

    private void Save()
    {
        PlayerPrefs.SetInt(GoldSaveKey, _currentGold);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        _currentGold = PlayerPrefs.GetInt(GoldSaveKey, 0);
    }
}