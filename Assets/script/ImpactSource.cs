using UnityEngine;
using DG.Tweening;

public class ImpactSource : MonoBehaviour, IKnockbackable, IHittable
{
    [SerializeField] private ImpactData _impactData;

    public ImpactData Data => _impactData;

    public void Hit()
    {
        OnKnocked();
    }

    public void OnKnocked()
    {
        transform
            .DOScale(Vector3.zero, 0.3f)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
    }
}