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

    private Transform target;
    private bool warnedAboutMissingReferences;

    public void SetPlayer(Transform playerTransform)
    {
        player = playerTransform;
    }

    public void Show(Transform deliveryTarget)
    {
        if (deliveryTarget == null)
        {
            Debug.LogWarning($"{nameof(CargoArrowUI)} on {name}: target is not assigned.");
            return;
        }

        EnsureReferences();

        target = deliveryTarget;

        if (arrowModel != null)
            arrowModel.gameObject.SetActive(true);
    }

    public void Hide()
    {
        target = null;

        if (arrowModel != null)
            arrowModel.gameObject.SetActive(false);
    }

    private void Awake()
    {
        EnsureReferences();
        Hide();
    }

    private void Update()
    {
        if (target == null)
            return;

        EnsureReferences();

        if (player == null || arrowModel == null)
        {
            WarnMissingReferencesOnce();
            return;
        }

        UpdatePositionAndRotation();
    }

    private void EnsureReferences()
    {
        if (arrowModel == null)
            arrowModel = transform; // если не задано — сам объект

        if (player == null)
        {
            Player foundPlayer = FindObjectOfType<Player>();
            if (foundPlayer != null)
                player = foundPlayer.transform;
        }
    }

    private void UpdatePositionAndRotation()
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
}