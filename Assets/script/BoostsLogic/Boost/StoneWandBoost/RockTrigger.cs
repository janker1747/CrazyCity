using UnityEngine;

public class RockTrigger : MonoBehaviour
{
    private ScoreSystem _scoreSystem;
    private RollingRock _rock;

    public void Init(ScoreSystem scoreSystem, RollingRock rock)
    {
        _scoreSystem = scoreSystem;
        _rock = rock;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_rock.IsDying)
            return;

        IHittable hittable = other.GetComponent<IHittable>();

        if (hittable != null)
        {
            hittable.Hit();
            _scoreSystem.AddScore(50);
        }
    }
}
