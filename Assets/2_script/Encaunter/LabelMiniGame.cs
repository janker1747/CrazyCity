using BoolAction = System.Action<bool>;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class LabelMiniGame : MonoBehaviour, IPointerDownHandler, IMiniGameController
{
    public event BoolAction Finished;

    [Header("Reference")]
    [SerializeField] private RectTransform label;

    [Header("Movement")]
    [SerializeField, Min(1f)] private float minSpeed = 300f;
    [SerializeField, Min(1f)] private float maxSpeed = 600f;

    [Tooltip("Запускать мини-игру при включении объекта")]
    [SerializeField] private bool startOnEnable = true;

    [Header("Result Check")]
    [Tooltip("Допустимое расстояние от исходной позиции по оси X")]
    [SerializeField, Min(0f)] private float positionTolerance = 60f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private RectTransform movementArea;

    private Vector3 correctPosition;

    private float leftEdge;
    private float rightEdge;
    private float currentSpeed;
    private float direction;

    private bool positionSaved;
    private bool isMoving;
    private bool attemptUsed;

    public bool IsMoving => isMoving;
    public float CurrentSpeed => currentSpeed;
    public float CurrentDistanceToTarget =>
        Mathf.Abs(label.localPosition.x - correctPosition.x);

    private void Awake()
    {
        if (label == null)
        {
            Debug.LogError(
                $"{nameof(LabelMiniGame)}: Label не назначен.",
                this
            );

            enabled = false;
            return;
        }

        movementArea = label.parent as RectTransform;

        if (movementArea == null)
        {
            Debug.LogError(
                $"{nameof(LabelMiniGame)}: родитель Label должен быть RectTransform.",
                this
            );

            enabled = false;
            return;
        }

        SaveCorrectPosition();
    }

    private void OnEnable()
    {
        if (startOnEnable && positionSaved)
            StartGame();
    }

    private void Update()
    {
        if (!isMoving)
            return;

        MoveLabel();
    }

    private void SaveCorrectPosition()
    {
        correctPosition = label.localPosition;
        positionSaved = true;

        if (showDebugLogs)
        {
            Debug.Log(
                $"[LabelMiniGame] Правильная позиция сохранена. " +
                $"X: {correctPosition.x:F1}",
                this
            );
        }
    }

    public void StartGame()
    {
        if (!positionSaved)
            SaveCorrectPosition();

        Canvas.ForceUpdateCanvases();

        CalculateScreenEdges();

        // Каждый запуск начинается с правильной позиции.
        label.localPosition = correctPosition;

        currentSpeed = Random.Range(
            Mathf.Min(minSpeed, maxSpeed),
            Mathf.Max(minSpeed, maxSpeed)
        );

        // -1 — движение влево, 1 — движение вправо.
        direction = Random.value < 0.5f ? -1f : 1f;

        attemptUsed = false;
        isMoving = true;

        if (showDebugLogs)
        {
            string directionName = direction < 0f
                ? "влево"
                : "вправо";

            Debug.Log(
                $"[LabelMiniGame] Игра запущена.\n" +
                $"Скорость: {currentSpeed:F1}\n" +
                $"Направление: {directionName}\n" +
                $"Левый край: {leftEdge:F1}\n" +
                $"Правый край: {rightEdge:F1}\n" +
                $"Погрешность: ±{positionTolerance:F1}",
                this
            );
        }
    }

    public void Begin()
    {
        if (!isMoving)
            StartGame();
    }

    private void CalculateScreenEdges()
    {
        Rect areaRect = movementArea.rect;

        float labelWidth =
            label.rect.width *
            Mathf.Abs(label.localScale.x);

        // Учитываем Pivot этикетки, чтобы она полностью
        // оставалась внутри экрана.
        leftEdge =
            areaRect.xMin +
            labelWidth * label.pivot.x;

        rightEdge =
            areaRect.xMax -
            labelWidth * (1f - label.pivot.x);

        if (leftEdge > rightEdge)
        {
            float center = areaRect.center.x;
            leftEdge = center;
            rightEdge = center;

            Debug.LogWarning(
                "[LabelMiniGame] Этикетка шире области движения.",
                this
            );
        }
    }

    private void MoveLabel()
    {
        Vector3 position = label.localPosition;

        position.x +=
            direction *
            currentSpeed *
            Time.unscaledDeltaTime;

        if (position.x <= leftEdge)
        {
            position.x = leftEdge;
            direction = 1f;

            if (showDebugLogs)
                Debug.Log("[LabelMiniGame] Достигнут левый край.", this);
        }
        else if (position.x >= rightEdge)
        {
            position.x = rightEdge;
            direction = -1f;

            if (showDebugLogs)
                Debug.Log("[LabelMiniGame] Достигнут правый край.", this);
        }

        label.localPosition = position;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        TryPlaceLabel();
    }

    public void TryPlaceLabel()
    {
        if (!isMoving || attemptUsed)
            return;

        attemptUsed = true;
        isMoving = false;

        float currentX = label.localPosition.x;
        float targetX = correctPosition.x;
        float distance = Mathf.Abs(currentX - targetX);

        bool success = distance <= positionTolerance;

        if (success)
        {
            Debug.Log(
                $"<color=green>[LabelMiniGame] SUCCESS</color>\n" +
                $"Текущая позиция: {currentX:F1}\n" +
                $"Правильная позиция: {targetX:F1}\n" +
                $"Расстояние: {distance:F1}\n" +
                $"Допустимая погрешность: {positionTolerance:F1}",
                this
            );
        }
        else
        {
            Debug.Log(
                $"<color=red>[LabelMiniGame] FAIL</color>\n" +
                $"Текущая позиция: {currentX:F1}\n" +
                $"Правильная позиция: {targetX:F1}\n" +
                $"Расстояние: {distance:F1}\n" +
                $"Допустимая погрешность: {positionTolerance:F1}",
                this
            );
        }

        Finished?.Invoke(success);
    }

    public void ResetLabel()
    {
        isMoving = false;
        attemptUsed = false;

        label.localPosition = correctPosition;

        if (showDebugLogs)
        {
            Debug.Log(
                "[LabelMiniGame] Этикетка возвращена в исходное положение.",
                this
            );
        }
    }

    public void RestartGame()
    {
        ResetLabel();
        StartGame();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (maxSpeed < minSpeed)
            maxSpeed = minSpeed;
    }
#endif
}
