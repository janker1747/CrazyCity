using System.Collections.Generic;
using UnityEngine;

public class GameData
{
    private const int CoinsPerCargo = 5;

    public static GameData Instance { get; } = new GameData();

    private readonly List<Cargo> _deliveredCargos = new List<Cargo>();

    private readonly List<string> _grades = new List<string>
    {
        "NOT BAD",
        "COOL",
        "OKAY",
        "NICE",
        "AWESOME"
    };

    private readonly Wallet _wallet = new Wallet();

    private CarItemSO _carItem;
    private string _grade;

    private int _lastRunCoins;
    private bool _runCoinsClaimed;

    public CarItemSO CarItem => _carItem;
    public string Grade => GetGrade();
    public string LastGrade => _grade;

    public Wallet Wallet => _wallet;

    public int DeliveredCargoCount => _deliveredCargos.Count;
    public int LastRunCoins => _lastRunCoins;

    public List<Sprite> Sprites => GetSprites();

    private GameData()
    {
    }

    public void SetCar(CarItemSO carItem)
    {
        _carItem = carItem;
    }

    public void AddCargo(Cargo cargo)
    {
        if (cargo == null)
            return;

        _deliveredCargos.Add(cargo);
    }

    /// <summary>
    /// Вызывает начисление монет за текущий заезд.
    /// Повторный вызов не начислит монеты второй раз.
    /// </summary>
    public int ClaimRunCoins()
    {
        if (_runCoinsClaimed)
            return _lastRunCoins;

        _lastRunCoins = CalculateRunCoins();
        _runCoinsClaimed = true;

        _wallet.AddGold(_lastRunCoins);

        return _lastRunCoins;
    }

    /// <summary>
    /// Только рассчитывает награду, ничего не начисляя.
    /// </summary>
    public int CalculateRunCoins()
    {
        int deliveredCount = _deliveredCargos.Count;

        if (deliveredCount <= 0)
            return 0;

        int coins = deliveredCount * CoinsPerCargo;
        coins += GetGradeCoinBonus(deliveredCount);

        return coins;
    }

    /// <summary>
    /// Нужно вызвать перед началом нового заезда.
    /// </summary>
    public void BeginNewRun()
    {
        _deliveredCargos.Clear();

        _lastRunCoins = 0;
        _runCoinsClaimed = false;
        _grade = null;
    }

    public void ClearDeliveredCargos()
    {
        BeginNewRun();
    }

    private int GetGradeCoinBonus(int deliveredCount)
    {
        if (deliveredCount >= 200)
            return 500;

        if (deliveredCount >= 100)
            return 200;

        if (deliveredCount >= 50)
            return 75;

        if (deliveredCount >= 30)
            return 25;

        if (deliveredCount >= 10)
            return 10;

        return 0;
    }

    private string GetGrade()
    {
        string grade = _grades[0];

        if (_deliveredCargos.Count >= 200)
            grade = "AWESOME";
        else if (_deliveredCargos.Count >= 100)
            grade = "NICE";
        else if (_deliveredCargos.Count >= 50)
            grade = "COOL";
        else if (_deliveredCargos.Count >= 30)
            grade = "OKAY";
        else if (_deliveredCargos.Count >= 10)
            grade = "NOT BAD";

        _grade = grade;

        return grade;
    }

    private List<Sprite> GetSprites()
    {
        List<Sprite> sprites = new List<Sprite>(_deliveredCargos.Count);

        foreach (Cargo cargo in _deliveredCargos)
        {
            if (cargo == null || cargo.Icon == null)
                continue;

            sprites.Add(cargo.Icon);
        }

        return sprites;
    }
}