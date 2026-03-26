using System;
using UnityEngine;

public class PlayerBoostSlot 
{
    private BoostData _current;

    public bool HasBoost => _current != null;

    public event Action<Sprite> BoostPickUP;

    public void Set(BoostData data)
    {
        _current = data;
        BoostPickUP?.Invoke(_current.sprite);
    }

    public BoostData Take()
    {
        var temp = _current;
        _current = null;
        return temp;
    }

    public BoostData Peek()
    {
        return _current;
    }
}