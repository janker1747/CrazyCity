using System;
using System.Collections.Generic;

public class BoostSystem 
{
    private List<ITimedBoost> _timedBoosts = new List<ITimedBoost>();
    private List<IEventBoost> _eventBoosts = new List<IEventBoost>();

    public void ActivateBoost(IBoost boost)
    {
        boost.Activate();

        if (boost is ITimedBoost timed)
        {
            _timedBoosts.Add(timed);
        }

        if (boost is IEventBoost ev)
        {
            ev.Subscribe();
            _eventBoosts.Add(ev);
        }
    }

    public void Update(float deltaTime)
    {
        for (int i = _timedBoosts.Count - 1; i >= 0; i--)
        {
            var boost = _timedBoosts[i];
            boost.Tick(deltaTime);

            if (boost.IsFinished)
            {
                boost.Deactivate();
                _timedBoosts.RemoveAt(i);
            }
        }
    }

    public void DeactivateAll()
    {
        for (int i = 0; i < _timedBoosts.Count; i++)
        {
            _timedBoosts[i].Deactivate();
        }

        for (int i = 0; i < _eventBoosts.Count; i++)
        {
            _eventBoosts[i].Unsubscribe();
        }

        _timedBoosts.Clear();
        _eventBoosts.Clear();
    }
}