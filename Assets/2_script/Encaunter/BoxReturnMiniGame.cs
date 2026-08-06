using BoolAction = System.Action<bool>;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public sealed class BoxReturnMiniGame : MonoBehaviour, IMiniGameController
{
    public event BoolAction Finished;

    private sealed class BoxState
    {
        public BoxDragItem item;

        // Правильное место коробки.
        public Vector3 targetWorldPosition;

        // Место, куда коробку разбросило в начале раунда.
        public Vector3 scatteredWorldPosition;

        public int originalSiblingIndex;

        public bool selected;
        public bool placed;
    }

    [Header("References")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Область, внутри которой коробки будут разбросаны.")]
    [SerializeField] private RectTransform scatterArea;

    [Tooltip("Все коробки мини-игры.")]
    [SerializeField] private List<RectTransform> boxes =
        new List<RectTransform>();

    [Header("Round")]
    [SerializeField, Min(1)] private int boxesPerRound = 3;

    [Tooltip("Допустимое отклонение от правильного места в пикселях.")]
    [SerializeField] private Vector2 snapTolerancePixels =
        new Vector2(80f, 80f);

    [Header("Scattering")]
    [SerializeField, Min(0f)]
    private float scatterAreaPaddingPixels = 40f;

    [Tooltip("Минимальное расстояние от исходного места коробки.")]
    [SerializeField, Min(0f)]
    private float minScatterDistancePixels = 250f;

    [Tooltip("Дополнительный свободный зазор между коробками.")]
    [SerializeField, Min(0f)]
    private float overlapPaddingPixels = 20f;

    [Tooltip("Количество случайных попыток найти свободную позицию.")]
    [SerializeField, Min(1)]
    private int randomPositionAttempts = 150;

    [Header("Snap animation")]
    [SerializeField, Min(0f)]
    private float snapDuration = 0.25f;

    [SerializeField]
    private Ease snapEase = Ease.OutQuad;

    [Header("Wrong position return")]
    [SerializeField, Min(0f)]
    private float returnDuration = 0.25f;

    [SerializeField]
    private Ease returnEase = Ease.OutQuad;

    [SerializeField]
    private bool useUnscaledTime = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onMiniGameStarted;
    [SerializeField] private UnityEvent onMiniGameCompleted;
    [SerializeField] private UnityEvent onBoxPlaced;

    private readonly Dictionary<BoxDragItem, BoxState> states =
        new Dictionary<BoxDragItem, BoxState>();

    // Все занятые области экрана:
    // невыбранные коробки и уже разбросанные коробки.
    private readonly List<Rect> occupiedScreenRects =
        new List<Rect>();

    private bool isRunning;
    private int requiredBoxes;
    private int placedBoxes;

    public bool IsRunning => isRunning;
    public int RequiredBoxes => requiredBoxes;
    public int PlacedBoxes => placedBoxes;

    private Camera UICamera
    {
        get
        {
            if (canvas == null ||
                canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return canvas.worldCamera;
        }
    }

    private void Awake()
    {
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (canvas != null)
            canvas = canvas.rootCanvas;
    }

    private void Start()
    {
        if (!isRunning)
            StartMiniGame();
    }

    public void StartMiniGame()
    {
        if (scatterArea == null)
        {
            Debug.LogError(
                $"{nameof(BoxReturnMiniGame)}: Scatter Area не назначена.",
                this);

            return;
        }

        if (canvas == null)
        {
            Debug.LogError(
                $"{nameof(BoxReturnMiniGame)}: Canvas не найден.",
                this);

            return;
        }

        if (boxes.Count == 0)
        {
            Debug.LogWarning(
                $"{nameof(BoxReturnMiniGame)}: список коробок пуст.",
                this);

            return;
        }

        if (isRunning)
            RestoreAllBoxesInstantly();

        // Обновляем позиции UI перед получением прямоугольников.
        Canvas.ForceUpdateCanvases();

        CaptureBoxPositions();

        List<BoxDragItem> availableBoxes =
            new List<BoxDragItem>(states.Keys);

        Shuffle(availableBoxes);

        int selectedAmount =
            Mathf.Min(boxesPerRound, availableBoxes.Count);

        placedBoxes = 0;

        // Сначала отмечаем выбранные коробки.
        // Это важно, чтобы их исходные позиции
        // не считались занятыми при разбросе.
        for (int i = 0; i < selectedAmount; i++)
        {
            BoxState state = states[availableBoxes[i]];

            state.selected = true;
            state.placed = false;
        }

        BuildOccupiedRects();

        int successfullyScattered = 0;

        for (int i = 0; i < selectedAmount; i++)
        {
            BoxDragItem item = availableBoxes[i];
            BoxState state = states[item];

            if (ScatterBox(state))
            {
                item.SetInteractable(true);
                successfullyScattered++;
            }
            else
            {
                state.selected = false;
                state.placed = false;

                item.SetInteractable(false);
                item.RectTransform.position =
                    state.targetWorldPosition;

                // Коробка осталась на исходном месте,
                // поэтому это место снова становится занятым.
                occupiedScreenRects.Add(
                    GetScreenRect(item.RectTransform));

                Debug.LogWarning(
                    $"Для коробки {item.name} не удалось найти " +
                    "свободное место. Увеличь Scatter Area.",
                    item);
            }
        }

        requiredBoxes = successfullyScattered;
        isRunning = requiredBoxes > 0;

        if (isRunning)
        {
            onMiniGameStarted?.Invoke();
        }
        else
        {
            Debug.LogWarning(
                $"{nameof(BoxReturnMiniGame)}: " +
                "не удалось разбросать коробки.",
                this);
        }
    }

    public void Begin()
    {
        if (!isRunning)
            StartMiniGame();
    }

    private void CaptureBoxPositions()
    {
        states.Clear();

        foreach (RectTransform box in boxes)
        {
            if (box == null)
                continue;

            box.DOKill();

            BoxDragItem dragItem =
                box.GetComponent<BoxDragItem>();

            if (dragItem == null)
                dragItem = box.gameObject.AddComponent<BoxDragItem>();

            dragItem.Initialize(this);
            dragItem.SetInteractable(false);

            BoxState state = new BoxState
            {
                item = dragItem,
                targetWorldPosition = box.position,
                scatteredWorldPosition = box.position,
                originalSiblingIndex = box.GetSiblingIndex(),
                selected = false,
                placed = false
            };

            states.Add(dragItem, state);
        }
    }

    /// <summary>
    /// Добавляет в список занятых мест все коробки,
    /// которые не участвуют в текущем раунде.
    /// </summary>
    private void BuildOccupiedRects()
    {
        occupiedScreenRects.Clear();

        foreach (BoxState state in states.Values)
        {
            if (state.selected)
                continue;

            RectTransform box = state.item.RectTransform;

            if (!box.gameObject.activeInHierarchy)
                continue;

            occupiedScreenRects.Add(GetScreenRect(box));
        }
    }

    private bool ScatterBox(BoxState state)
    {
        RectTransform box = state.item.RectTransform;

        Vector2 targetScreenPosition =
            RectTransformUtility.WorldToScreenPoint(
                UICamera,
                state.targetWorldPosition);

        if (!TryGetScatterPosition(
                box,
                targetScreenPosition,
                out Vector2 scatterScreenPosition,
                out Rect scatterScreenRect))
        {
            return false;
        }

        RectTransform parent = box.parent as RectTransform;

        if (parent == null)
            return false;

        if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(
                parent,
                scatterScreenPosition,
                UICamera,
                out Vector3 worldPosition))
        {
            return false;
        }

        box.position = worldPosition;

        state.scatteredWorldPosition = worldPosition;

        // Теперь эта область занята,
        // поэтому следующая коробка не сможет сюда попасть.
        occupiedScreenRects.Add(scatterScreenRect);

        return true;
    }

    private bool TryGetScatterPosition(
        RectTransform box,
        Vector2 targetScreenPosition,
        out Vector2 resultPosition,
        out Rect resultRect)
    {
        Rect areaRect = GetScreenRect(scatterArea);
        Vector2 boxSize = GetScreenRect(box).size;
        Vector2 halfSize = boxSize * 0.5f;

        float minX =
            areaRect.xMin +
            halfSize.x +
            scatterAreaPaddingPixels;

        float maxX =
            areaRect.xMax -
            halfSize.x -
            scatterAreaPaddingPixels;

        float minY =
            areaRect.yMin +
            halfSize.y +
            scatterAreaPaddingPixels;

        float maxY =
            areaRect.yMax -
            halfSize.y -
            scatterAreaPaddingPixels;

        if (minX > maxX || minY > maxY)
        {
            resultPosition = default;
            resultRect = default;

            return false;
        }

        // Сначала пробуем случайные позиции.
        for (int attempt = 0;
             attempt < randomPositionAttempts;
             attempt++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(minX, maxX),
                Random.Range(minY, maxY));

            if (IsScatterPositionValid(
                    candidate,
                    boxSize,
                    targetScreenPosition,
                    out Rect candidateRect))
            {
                resultPosition = candidate;
                resultRect = candidateRect;

                return true;
            }
        }

        // Если случайная позиция не найдена,
        // сканируем доступную область по сетке.
        float stepX = Mathf.Max(20f, boxSize.x * 0.25f);
        float stepY = Mathf.Max(20f, boxSize.y * 0.25f);

        bool reverseX = Random.value > 0.5f;
        bool reverseY = Random.value > 0.5f;

        if (reverseY)
        {
            for (float y = maxY; y >= minY; y -= stepY)
            {
                if (TryFindPositionInRow(
                        y,
                        minX,
                        maxX,
                        stepX,
                        reverseX,
                        boxSize,
                        targetScreenPosition,
                        out resultPosition,
                        out resultRect))
                {
                    return true;
                }
            }
        }
        else
        {
            for (float y = minY; y <= maxY; y += stepY)
            {
                if (TryFindPositionInRow(
                        y,
                        minX,
                        maxX,
                        stepX,
                        reverseX,
                        boxSize,
                        targetScreenPosition,
                        out resultPosition,
                        out resultRect))
                {
                    return true;
                }
            }
        }

        resultPosition = default;
        resultRect = default;

        return false;
    }

    private bool TryFindPositionInRow(
        float y,
        float minX,
        float maxX,
        float stepX,
        bool reverseX,
        Vector2 boxSize,
        Vector2 targetScreenPosition,
        out Vector2 resultPosition,
        out Rect resultRect)
    {
        if (reverseX)
        {
            for (float x = maxX; x >= minX; x -= stepX)
            {
                Vector2 candidate = new Vector2(x, y);

                if (IsScatterPositionValid(
                        candidate,
                        boxSize,
                        targetScreenPosition,
                        out Rect candidateRect))
                {
                    resultPosition = candidate;
                    resultRect = candidateRect;

                    return true;
                }
            }
        }
        else
        {
            for (float x = minX; x <= maxX; x += stepX)
            {
                Vector2 candidate = new Vector2(x, y);

                if (IsScatterPositionValid(
                        candidate,
                        boxSize,
                        targetScreenPosition,
                        out Rect candidateRect))
                {
                    resultPosition = candidate;
                    resultRect = candidateRect;

                    return true;
                }
            }
        }

        resultPosition = default;
        resultRect = default;

        return false;
    }

    private bool IsScatterPositionValid(
        Vector2 candidatePosition,
        Vector2 boxSize,
        Vector2 targetScreenPosition,
        out Rect candidateRect)
    {
        candidateRect = CreateCenteredRect(
            candidatePosition,
            boxSize);

        if (Vector2.Distance(
                candidatePosition,
                targetScreenPosition) <
            minScatterDistancePixels)
        {
            return false;
        }

        Rect paddedCandidateRect =
            ExpandRect(
                candidateRect,
                overlapPaddingPixels);

        foreach (Rect occupiedRect in occupiedScreenRects)
        {
            if (paddedCandidateRect.Overlaps(occupiedRect))
                return false;
        }

        return true;
    }

    internal void OnBoxReleased(BoxDragItem item)
    {
        if (!isRunning)
            return;

        if (!states.TryGetValue(item, out BoxState state))
            return;

        if (!state.selected || state.placed)
            return;

        Vector2 currentScreenPosition =
            RectTransformUtility.WorldToScreenPoint(
                UICamera,
                item.RectTransform.position);

        Vector2 targetScreenPosition =
            RectTransformUtility.WorldToScreenPoint(
                UICamera,
                state.targetWorldPosition);

        Vector2 difference =
            currentScreenPosition -
            targetScreenPosition;

        bool insideTolerance =
            Mathf.Abs(difference.x) <=
            snapTolerancePixels.x &&
            Mathf.Abs(difference.y) <=
            snapTolerancePixels.y;

        if (insideTolerance)
        {
            SnapBoxToTarget(state);
        }
        else
        {
            ReturnBoxToScatteredPosition(state);
        }
    }

    /// <summary>
    /// Неправильно поставленная коробка возвращается
    /// туда, куда её разбросило в начале раунда.
    /// </summary>
    private void ReturnBoxToScatteredPosition(BoxState state)
    {
        state.item.SetInteractable(false);

        RectTransform box = state.item.RectTransform;

        box.DOKill();

        box.DOMove(
                state.scatteredWorldPosition,
                returnDuration)
            .SetEase(returnEase)
            .SetUpdate(useUnscaledTime)
            .OnComplete(() =>
            {
                if (isRunning && !state.placed)
                    state.item.SetInteractable(true);
            });
    }

    private void SnapBoxToTarget(BoxState state)
    {
        state.placed = true;
        state.item.SetInteractable(false);

        RectTransform box = state.item.RectTransform;

        box.DOKill();

        box.DOMove(
                state.targetWorldPosition,
                snapDuration)
            .SetEase(snapEase)
            .SetUpdate(useUnscaledTime)
            .OnComplete(() => OnBoxSnapCompleted(state));
    }

    private void OnBoxSnapCompleted(BoxState state)
    {
        placedBoxes++;

        onBoxPlaced?.Invoke();

        if (placedBoxes < requiredBoxes)
            return;

        isRunning = false;

        RestoreOriginalSiblingOrder();
        onMiniGameCompleted?.Invoke();
        Finished?.Invoke(true);
    }

    public void StopMiniGame()
    {
        RestoreAllBoxesInstantly();
        isRunning = false;
    }

    private void RestoreAllBoxesInstantly()
    {
        foreach (BoxState state in states.Values)
        {
            RectTransform box = state.item.RectTransform;

            box.DOKill();
            box.position = state.targetWorldPosition;

            state.item.SetInteractable(false);

            state.selected = false;
            state.placed = false;
        }

        RestoreOriginalSiblingOrder();

        requiredBoxes = 0;
        placedBoxes = 0;
    }

    private void RestoreOriginalSiblingOrder()
    {
        List<BoxState> orderedStates =
            new List<BoxState>(states.Values);

        orderedStates.Sort(
            (a, b) =>
                a.originalSiblingIndex.CompareTo(
                    b.originalSiblingIndex));

        foreach (BoxState state in orderedStates)
        {
            state.item.RectTransform.SetSiblingIndex(
                state.originalSiblingIndex);
        }
    }

    private Rect GetScreenRect(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 screenPoint =
                RectTransformUtility.WorldToScreenPoint(
                    UICamera,
                    corners[i]);

            minX = Mathf.Min(minX, screenPoint.x);
            minY = Mathf.Min(minY, screenPoint.y);
            maxX = Mathf.Max(maxX, screenPoint.x);
            maxY = Mathf.Max(maxY, screenPoint.y);
        }

        return Rect.MinMaxRect(
            minX,
            minY,
            maxX,
            maxY);
    }

    private static Rect CreateCenteredRect(
        Vector2 center,
        Vector2 size)
    {
        return new Rect(
            center - size * 0.5f,
            size);
    }

    private static Rect ExpandRect(
        Rect rect,
        float amount)
    {
        return Rect.MinMaxRect(
            rect.xMin - amount,
            rect.yMin - amount,
            rect.xMax + amount,
            rect.yMax + amount);
    }

    private static void Shuffle(List<BoxDragItem> items)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int randomIndex =
                Random.Range(0, i + 1);

            BoxDragItem temporary = items[i];
            items[i] = items[randomIndex];
            items[randomIndex] = temporary;
        }
    }
}
