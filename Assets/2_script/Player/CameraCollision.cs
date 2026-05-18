using UnityEngine;

public class CameraCollision : MonoBehaviour
{
    public Transform target;
    public float distance = 4f;
    public float minDistance = 0.5f;
    public float smooth = 10f;
    public LayerMask collisionMask;

    private float currentDistance;

    void Start()
    {
        currentDistance = distance;
    }

    void LateUpdate()
    {
        Vector3 dir = (transform.position - target.position).normalized;

        if (Physics.Raycast(target.position, dir, out RaycastHit hit, distance, collisionMask))
            currentDistance = Mathf.Clamp(hit.distance, minDistance, distance);
        else
            currentDistance = Mathf.Lerp(currentDistance, distance, Time.deltaTime * smooth);

        transform.position = target.position + dir * currentDistance;
    }
}
