using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MiniGameRewardPanelInstaller
{
    private const string ScenePath = "Assets/1_GameLoopScene/MiniGame.unity";

    [MenuItem("Crazy City/Mini Games/Rebuild Reward Panel")]
    public static void ConfigureScene()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedForConfiguration = !scene.IsValid() || !scene.isLoaded;

        if (openedForConfiguration)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        Canvas canvas = FindInScene<Canvas>(scene);
        MiniGameSceneManager manager = FindInScene<MiniGameSceneManager>(scene);

        if (canvas == null || manager == null)
            throw new MissingReferenceException("MiniGame scene requires Canvas and MiniGameSceneManager.");

        Transform existing = canvas.transform.Find("RewardPanel");
        GameObject panel = existing != null
            ? existing.gameObject
            : CreateUiObject("RewardPanel", canvas.transform);

        ClearPanel(panel);
        panel.layer = 5;

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        Stretch(panelRect);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.035f, 0.045f, 0.075f, 0.97f);

        MiniGameRewardPanelView view = panel.AddComponent<MiniGameRewardPanelView>();

        Text title = CreateText(
            "Title",
            panel.transform,
            "MINI-GAME COMPLETE",
            70f,
            Color.white,
            new Vector2(0.12f, 0.78f),
            new Vector2(0.88f, 0.94f));

        GameObject successContent = CreateUiObject("SuccessContent", panel.transform);
        Stretch(successContent.GetComponent<RectTransform>());

        Text walletReward = CreateText(
            "WalletReward",
            successContent.transform,
            "WALLET  +50 COINS",
            52f,
            new Color(1f, 0.82f, 0.18f),
            new Vector2(0.25f, 0.65f),
            new Vector2(0.75f, 0.76f));

        string[] categories = { "REGULAR", "TIMED", "HEALTH" };
        GameObject[] cardRoots = new GameObject[3];
        Image[] cardIcons = new Image[3];
        Text[] cardNames = new Text[3];

        for (int i = 0; i < categories.Length; i++)
        {
            float minX = 0.09f + i * 0.29f;
            float maxX = minX + 0.24f;
            CreateCargoCard(
                successContent.transform,
                categories[i],
                new Vector2(minX, 0.27f),
                new Vector2(maxX, 0.62f),
                out cardRoots[i],
                out cardIcons[i],
                out cardNames[i]);
        }

        GameObject failureContent = CreateUiObject("FailureContent", panel.transform);
        Stretch(failureContent.GetComponent<RectTransform>());

        Text penaltyText = CreateText(
            "ScorePenalty",
            failureContent.transform,
            "-50 SCORE",
            60f,
            new Color(1f, 0.3f, 0.25f),
            new Vector2(0.2f, 0.34f),
            new Vector2(0.8f, 0.70f));

        Button continueButton = CreateButton(
            "ContinueButton",
            panel.transform,
            "CONTINUE",
            new Color(0.15f, 0.55f, 0.92f, 1f),
            new Vector2(0.36f, 0.09f),
            new Vector2(0.64f, 0.22f));

        SerializedObject viewObject = new SerializedObject(view);
        viewObject.FindProperty("panelRoot").objectReferenceValue = panel;
        viewObject.FindProperty("titleText").objectReferenceValue = title;
        viewObject.FindProperty("successTitleColor").colorValue = new Color(0.35f, 1f, 0.45f);
        viewObject.FindProperty("failureTitleColor").colorValue = new Color(1f, 0.3f, 0.25f);
        viewObject.FindProperty("continueButton").objectReferenceValue = continueButton;
        viewObject.FindProperty("successContent").objectReferenceValue = successContent;
        viewObject.FindProperty("walletRewardText").objectReferenceValue = walletReward;
        viewObject.FindProperty("failureContent").objectReferenceValue = failureContent;
        viewObject.FindProperty("scorePenaltyText").objectReferenceValue = penaltyText;

        SerializedProperty slots = viewObject.FindProperty("cargoSlots");
        slots.arraySize = 3;
        for (int i = 0; i < slots.arraySize; i++)
        {
            SerializedProperty slot = slots.GetArrayElementAtIndex(i);
            slot.FindPropertyRelative("root").objectReferenceValue = cardRoots[i];
            slot.FindPropertyRelative("icon").objectReferenceValue = cardIcons[i];
            slot.FindPropertyRelative("cargoName").objectReferenceValue = cardNames[i];
        }
        viewObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject managerObject = new SerializedObject(manager);
        managerObject.FindProperty("minTimeLimitSeconds").floatValue = 5f;
        managerObject.FindProperty("maxTimeLimitSeconds").floatValue = 10f;
        managerObject.FindProperty("rewardPanelView").objectReferenceValue = view;
        managerObject.ApplyModifiedPropertiesWithoutUndo();

        failureContent.SetActive(false);
        panel.SetActive(false);

        EditorUtility.SetDirty(panel);
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        if (openedForConfiguration)
            EditorSceneManager.CloseScene(scene, true);

        Debug.Log("MiniGame RewardPanel was rebuilt as editable scene UI.");
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            T component = roots[i].GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }

        return null;
    }

    private static void ClearPanel(GameObject panel)
    {
        Transform transform = panel.transform;
        while (transform.childCount > 0)
            Object.DestroyImmediate(transform.GetChild(0).gameObject);

        Component[] components = panel.GetComponents<Component>();
        for (int i = components.Length - 1; i >= 0; i--)
        {
            if (!(components[i] is RectTransform))
                Object.DestroyImmediate(components[i]);
        }
    }

    private static void CreateCargoCard(
        Transform parent,
        string category,
        Vector2 anchorMin,
        Vector2 anchorMax,
        out GameObject card,
        out Image icon,
        out Text cargoName)
    {
        card = CreateUiObject($"CargoReward_{category}", parent);
        Image background = card.AddComponent<Image>();
        background.color = new Color(0.12f, 0.16f, 0.25f, 1f);
        SetAnchors(card.GetComponent<RectTransform>(), anchorMin, anchorMax);

        CreateText(
            "Category",
            card.transform,
            category,
            26f,
            new Color(0.45f, 0.78f, 1f),
            new Vector2(0.04f, 0.82f),
            new Vector2(0.96f, 0.98f));

        GameObject iconObject = CreateUiObject("CargoIcon", card.transform);
        icon = iconObject.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        SetAnchors(icon.rectTransform, new Vector2(0.15f, 0.25f), new Vector2(0.85f, 0.80f));

        cargoName = CreateText(
            "CargoName",
            card.transform,
            "Cargo",
            28f,
            Color.white,
            new Vector2(0.04f, 0.03f),
            new Vector2(0.96f, 0.24f));
    }

    private static Button CreateButton(
        string objectName,
        Transform parent,
        string label,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        GameObject gameObject = CreateUiObject(objectName, parent);
        Image image = gameObject.AddComponent<Image>();
        image.color = color;

        Button button = gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        SetAnchors(gameObject.GetComponent<RectTransform>(), anchorMin, anchorMax);

        CreateText(
            "Label",
            gameObject.transform,
            label,
            38f,
            Color.white,
            Vector2.zero,
            Vector2.one);

        return button;
    }

    private static Text CreateText(
        string objectName,
        Transform parent,
        string value,
        float fontSize,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        GameObject gameObject = CreateUiObject(objectName, parent);
        Text text = gameObject.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = Mathf.RoundToInt(fontSize);
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 16;
        text.resizeTextMaxSize = Mathf.RoundToInt(fontSize);
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
        SetAnchors(text.rectTransform, anchorMin, anchorMax);
        return text;
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        gameObject.layer = 5;
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        SetAnchors(rectTransform, Vector2.zero, Vector2.one);
    }

    private static void SetAnchors(
        RectTransform rectTransform,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
