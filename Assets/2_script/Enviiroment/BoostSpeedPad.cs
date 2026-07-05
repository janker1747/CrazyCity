
using UnityEngine;

public class BoostSpeedPad : MonoBehaviour
{
    [SerializeField] private float _force;
    [SerializeField] private UnityEngine.ForceMode _forceMode;

    private Player _player;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (_player == null)
            {
            _player = other.GetComponent<Player>();
            }
            
            ApplyBoostDash(other, _player);
        }

    }

    private void ApplyBoostDash(Collider collision,Player player)
    {
        float knockbackForce = _force ;

        Vector3 direction = player.transform.forward;

        Vector3 force = direction * knockbackForce + Vector3.up * (knockbackForce * 0.5f);
        _player.UI.ActivateUiSpeedBoost();

        collision.GetComponent<Rigidbody>().AddForce(force, _forceMode);
    }
}
