using System.Collections.Generic;
using UnityEngine;

public class CargoArrowUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform arrowModel; // 3D стрелка (модель)

    [Header("Position")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 3f, 0f);

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 10f; // плавность поворота
    [SerializeField] private bool trackNearestCargoPickup = true;
    [SerializeField, Min(0.02f)] private float targetRefreshInterval = 0.15f;

    private readonly List<Transform> targets = new();
    private Transform cachedTarget;
    private float nextTargetRefreshTime;
    private float nextReferenceSearchTime;
    private bool warnedAboutMissingReferences;

    public void SetPlayer(Transform playerTransform)
    {
        player = playerTransform;
        InvalidateTargetCache();
    }

    public void Show(Transform deliveryTarget)
    {
        if (deliveryTarget == null)
        {
            Debug.LogWarning($"{nameof(CargoArrowUI)} on {name}: target is not assigned.");
            return;
        }

        EnsureReferences();
        trackNearestCargoPickup = false;

        if (!targets.Contains(deliveryTarget))
            targets.Add(deliveryTarget);

        InvalidateTargetCache();

        if (arrowModel != null)
            arrowModel.gameObject.SetActive(true);
    }

    public void Hide()
    {
        targets.Clear();
        trackNearestCargoPickup = true;
        InvalidateTargetCache();

        if (arrowModel != null)
            arrowModel.gameObject.SetActive(false);
    }

    private void Awake()
    {
        EnsureReferences();
        Hide();
    }

    private void LateUpdate()
    {
        EnsureReferences();

        if (player == null || arrowModel == null)
        {
            WarnMissingReferencesOnce();
            return;
        }

        if (cachedTarget == null ||
            !cachedTarget.gameObject.activeInHierarchy ||
            Time.unscaledTime >= nextTargetRefreshTime)
        {
            cachedTarget = trackNearestCargoPickup ? GetNearestCargoPickup() : GetNearestTarget();
            nextTargetRefreshTime = Time.unscaledTime + targetRefreshInterval;
        }

        if (cachedTarget == null)
        {
            arrowModel.gameObject.SetActive(false);
            return;
        }

        arrowModel.gameObject.SetActive(true);
        UpdatePositionAndRotation(cachedTarget);
    }

    private void EnsureReferences()
    {
        if (arrowModel == null)
            arrowModel = transform; // если не задано — сам объект

        if (player == null && Time.unscaledTime >= nextReferenceSearchTime)
        {
            nextReferenceSearchTime = Time.unscaledTime + 0.5f;
            Player foundPlayer = FindObjectOfType<Player>();
            if (foundPlayer != null)
                player = foundPlayer.transform;
        }
    }

    private Transform GetNearestTarget()
    {
        if (targets.Count == 0)
            return null;

        Transform nearestTarget = null;
        float nearestDistance = float.MaxValue;

        for (int i = targets.Count - 1; i >= 0; i--)
        {
            Transform candidate = targets[i];
            if (candidate == null)
            {
                targets.RemoveAt(i);
                continue;
            }

            float distance = (candidate.position - player.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestTarget = candidate;
            }
        }

        return nearestTarget;
    }

    private Transform GetNearestCargoPickup()
    {
        Transform nearestTarget = null;
        float nearestDistance = float.MaxValue;
        IReadOnlyList<CargoPickup> pickups = CargoPickup.ActivePickups;

        for (int i = 0; i < pickups.Count; i++)
        {
            CargoPickup pickup = pickups[i];
            if (pickup == null || !pickup.IsAvailable)
                continue;

            float distance = (pickup.transform.position - player.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestTarget = pickup.transform;
            }
        }

        return nearestTarget;
    }

    private void UpdatePositionAndRotation(Transform target)
    {
        // Позиция над игроком
        transform.position = player.position + worldOffset;

        // Направление к цели
        Vector3 direction = target.position - player.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Плавный поворот
        arrowModel.rotation = Quaternion.Lerp(
            arrowModel.rotation,
            targetRotation,
            Time.deltaTime * rotationSpeed
        );
    }

    private void WarnMissingReferencesOnce()
    {
        if (warnedAboutMissingReferences)
            return;

        Debug.LogWarning($"{nameof(CargoArrowUI)} on {name}: player or arrowModel is missing.");
        warnedAboutMissingReferences = true;
    }

    private void InvalidateTargetCache()
    {
        cachedTarget = null;
        nextTargetRefreshTime = 0f;
    }
}
