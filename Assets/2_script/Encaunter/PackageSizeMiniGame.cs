using BoolAction = System.Action<bool>;
using UnityEngine;
using UnityEngine.UI;

public class PackageSizeMiniGame : MonoBehaviour, IMiniGameController
{
    public event BoolAction Finished;

    private enum PackageSize
    {
        Small,
        Medium,
        Large
    }

    [Header("Предмет")]
    [SerializeField] private Image itemImage;

    [Tooltip("Ровно 4 спрайта предметов")]
    [SerializeField] private Sprite[] itemSprites = new Sprite[4];

    [Header("Кнопки коробок")]
    [SerializeField] private Button smallBoxButton;
    [SerializeField] private Button mediumBoxButton;
    [SerializeField] private Button largeBoxButton;

    [Header("Размеры предмета")]
    [SerializeField] private Vector2 smallItemSize = new(200f, 200f);
    [SerializeField] private Vector2 mediumItemSize = new(300f, 300f);
    [SerializeField] private Vector2 largeItemSize = new(400f, 400f);

    [Header("Размеры кнопок коробок")]
    [SerializeField] private Vector2 smallBoxSize = new(230f, 230f);
    [SerializeField] private Vector2 mediumBoxSize = new(320f, 320f);
    [SerializeField] private Vector2 largeBoxSize = new(430f, 430f);

    [Header("Запуск")]
    [SerializeField] private bool startOnEnable = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private PackageSize correctSize;
    private bool attemptUsed;
    private bool isRunning;

    private void Awake()
    {
        smallBoxButton.onClick.AddListener(ChooseSmallBox);
        mediumBoxButton.onClick.AddListener(ChooseMediumBox);
        largeBoxButton.onClick.AddListener(ChooseLargeBox);

        ApplyBoxButtonSizes();
    }

    private void OnEnable()
    {
        if (startOnEnable)
            StartGame();
    }

    private void OnDestroy()
    {
        smallBoxButton.onClick.RemoveListener(ChooseSmallBox);
        mediumBoxButton.onClick.RemoveListener(ChooseMediumBox);
        largeBoxButton.onClick.RemoveListener(ChooseLargeBox);
    }

    public void StartGame()
    {
        if (!ValidateReferences())
            return;

        attemptUsed = false;
        isRunning = true;

        SetButtonsInteractable(true);
        ApplyBoxButtonSizes();

        SelectRandomItem();
        SelectRandomItemSize();

        if (showDebugLogs)
        {
            Debug.Log(
                $"[PackageSizeMiniGame] Игра запущена.\n" +
                $"Правильная коробка: {correctSize}\n" +
                $"Размер предмета: {itemImage.rectTransform.sizeDelta}",
                this
            );
        }
    }

    public void Begin()
    {
        if (!isRunning)
            StartGame();
    }

    private void SelectRandomItem()
    {
        int index = Random.Range(0, itemSprites.Length);

        itemImage.sprite = itemSprites[index];
        itemImage.preserveAspect = true;

        if (showDebugLogs)
        {
            Debug.Log(
                $"[PackageSizeMiniGame] Выбран предмет с индексом {index}.",
                this
            );
        }
    }

    private void SelectRandomItemSize()
    {
        correctSize = (PackageSize)Random.Range(0, 3);

        itemImage.rectTransform.sizeDelta = correctSize switch
        {
            PackageSize.Small => smallItemSize,
            PackageSize.Medium => mediumItemSize,
            PackageSize.Large => largeItemSize,
            _ => mediumItemSize
        };
    }

    private void ApplyBoxButtonSizes()
    {
        if (smallBoxButton != null)
            GetButtonRect(smallBoxButton).sizeDelta = smallBoxSize;

        if (mediumBoxButton != null)
            GetButtonRect(mediumBoxButton).sizeDelta = mediumBoxSize;

        if (largeBoxButton != null)
            GetButtonRect(largeBoxButton).sizeDelta = largeBoxSize;
    }

    private static RectTransform GetButtonRect(Button button)
    {
        return button.transform as RectTransform;
    }

    public void ChooseSmallBox()
    {
        ChooseBox(PackageSize.Small);
    }

    public void ChooseMediumBox()
    {
        ChooseBox(PackageSize.Medium);
    }

    public void ChooseLargeBox()
    {
        ChooseBox(PackageSize.Large);
    }

    private void ChooseBox(PackageSize selectedSize)
    {
        if (attemptUsed)
            return;

        attemptUsed = true;
        isRunning = false;
        SetButtonsInteractable(false);

        bool success = selectedSize == correctSize;

        if (success)
        {
            Debug.Log(
                $"<color=green>[PackageSizeMiniGame] SUCCESS</color>\n" +
                $"Выбрана коробка: {selectedSize}\n" +
                $"Правильная коробка: {correctSize}",
                this
            );
        }
        else
        {
            Debug.Log(
                $"<color=red>[PackageSizeMiniGame] FAIL</color>\n" +
                $"Выбрана коробка: {selectedSize}\n" +
                $"Правильная коробка: {correctSize}",
                this
            );
        }

        Finished?.Invoke(success);
    }

    private void SetButtonsInteractable(bool value)
    {
        smallBoxButton.interactable = value;
        mediumBoxButton.interactable = value;
        largeBoxButton.interactable = value;
    }

    public void RestartGame()
    {
        StartGame();
    }

    private bool ValidateReferences()
    {
        if (itemImage == null)
        {
            Debug.LogError(
                "[PackageSizeMiniGame] Item Image не назначен.",
                this
            );

            return false;
        }

        if (smallBoxButton == null ||
            mediumBoxButton == null ||
            largeBoxButton == null)
        {
            Debug.LogError(
                "[PackageSizeMiniGame] Не назначены кнопки коробок.",
                this
            );

            return false;
        }

        if (itemSprites == null || itemSprites.Length == 0)
        {
            Debug.LogError(
                "[PackageSizeMiniGame] Нет спрайтов предметов.",
                this
            );

            return false;
        }

        for (int i = 0; i < itemSprites.Length; i++)
        {
            if (itemSprites[i] == null)
            {
                Debug.LogError(
                    $"[PackageSizeMiniGame] Спрайт предмета {i} не назначен.",
                    this
                );

                return false;
            }
        }

        return true;
    }
}
