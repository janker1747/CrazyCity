using System.Collections.Generic;
using UnityEngine;

public class CarSelectionManager : MonoBehaviour
{
    public static CarSelectionManager Instance;

    [SerializeField] private List<CarItemSO> _cars;

    private int _currentIndex;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void NextCar()
    {
        _currentIndex++;

        if (_currentIndex >= _cars.Count)
            _currentIndex = 0;
    }

    public void PreviousCar()
    {
        _currentIndex--;

        if (_currentIndex < 0)
            _currentIndex = _cars.Count - 1;
    }

    public CarItemSO GetCurrentCar()
    {
        return _cars[_currentIndex];
    }

    public Player GetPlayerPrefab()
    {
        return _cars[_currentIndex].PlayerPrefab;
    }
}