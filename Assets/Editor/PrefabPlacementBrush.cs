using UnityEditor;
using UnityEngine;

public class PrefabPlacementBrush : EditorWindow
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private LayerMask placementMask = ~0;

    private GameObject currentObject;
    private bool isRotating;

    [MenuItem("Tools/Prefab Placement Brush")]
    public static void Open()
    {
        GetWindow<PrefabPlacementBrush>("Prefab Brush");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;

        if (currentObject != null && isRotating)
        {
            DestroyImmediate(currentObject);
        }
    }

    private void OnGUI()
    {
        prefab = (GameObject)EditorGUILayout.ObjectField(
            "Prefab",
            prefab,
            typeof(GameObject),
            false
        );

        placementMask = LayerMaskField("Placement Mask", placementMask);

        EditorGUILayout.HelpBox(
            "ЛКМ 1 раз — поставить prefab.\n" +
            "Двигай мышкой — prefab вращается по Y.\n" +
            "ЛКМ 2 раз — зафиксировать поворот.\n" +
            "ESC — отменить текущий prefab.",
            MessageType.Info
        );
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;

        if (prefab == null)
            return;

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 5000f, placementMask))
            return;

        Handles.color = Color.green;
        Handles.DrawWireDisc(hit.point, hit.normal, 0.5f);

        if (isRotating && currentObject != null)
        {
            RotateObjectTowardsMouse(hit.point);
        }

        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            if (!isRotating)
            {
                PlacePrefab(hit.point, hit.normal);
            }
            else
            {
                ConfirmRotation();
            }

            e.Use();
        }

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            CancelCurrent();
            e.Use();
        }

        sceneView.Repaint();
    }

    private void PlacePrefab(Vector3 position, Vector3 normal)
    {
        currentObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        Undo.RegisterCreatedObjectUndo(currentObject, "Place Prefab");

        currentObject.transform.position = position;

        // Если нужно ставить строго вверх, оставь так:
        currentObject.transform.rotation = Quaternion.identity;

        // Если хочешь выравнивание по поверхности, замени строку выше на:
        // currentObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);

        isRotating = true;

        Selection.activeGameObject = currentObject;
    }

    private void RotateObjectTowardsMouse(Vector3 mouseWorldPoint)
    {
        Vector3 direction = mouseWorldPoint - currentObject.transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        currentObject.transform.rotation = targetRotation;
    }

    private void ConfirmRotation()
    {
        currentObject = null;
        isRotating = false;
    }

    private void CancelCurrent()
    {
        if (currentObject != null)
        {
            DestroyImmediate(currentObject);
        }

        currentObject = null;
        isRotating = false;
    }

    private static LayerMask LayerMaskField(string label, LayerMask selected)
    {
        var layers = UnityEditorInternal.InternalEditorUtility.layers;

        int maskWithoutEmpty = 0;

        for (int i = 0; i < layers.Length; i++)
        {
            int layer = LayerMask.NameToLayer(layers[i]);

            if (((1 << layer) & selected.value) != 0)
            {
                maskWithoutEmpty |= 1 << i;
            }
        }

        maskWithoutEmpty = EditorGUILayout.MaskField(label, maskWithoutEmpty, layers);

        int mask = 0;

        for (int i = 0; i < layers.Length; i++)
        {
            if ((maskWithoutEmpty & (1 << i)) != 0)
            {
                mask |= 1 << LayerMask.NameToLayer(layers[i]);
            }
        }

        selected.value = mask;
        return selected;
    }
}