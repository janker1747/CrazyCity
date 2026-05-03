using UnityEditor;
using UnityEngine;

public class PrefabPainterTool : EditorWindow
{
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private LayerMask placementLayer = ~0;
    [SerializeField] private bool randomPrefab = true;
    [SerializeField] private float yOffset = 0f;
    [SerializeField] private bool randomRotationY = true;

    private int selectedPrefabIndex;
    private bool isPainting;

    [MenuItem("Tools/Prefab Painter")]
    public static void Open()
    {
        GetWindow<PrefabPainterTool>("Prefab Painter");
    }

    private void OnGUI()
    {
        SerializedObject so = new SerializedObject(this);

        EditorGUILayout.PropertyField(so.FindProperty("prefabs"), true);
        EditorGUILayout.PropertyField(so.FindProperty("placementLayer"));
        EditorGUILayout.PropertyField(so.FindProperty("randomPrefab"));

        if (!randomPrefab && prefabs != null && prefabs.Length > 0)
        {
            selectedPrefabIndex = EditorGUILayout.IntSlider(
                "Prefab Index",
                selectedPrefabIndex,
                0,
                prefabs.Length - 1
            );
        }

        yOffset = EditorGUILayout.FloatField("Y Offset", yOffset);
        randomRotationY = EditorGUILayout.Toggle("Random Rotation Y", randomRotationY);

        GUILayout.Space(10);

        GUI.backgroundColor = isPainting ? Color.green : Color.white;

        if (GUILayout.Button(isPainting ? "Painting ON" : "Painting OFF"))
        {
            isPainting = !isPainting;
            SceneView.RepaintAll();
        }

        GUI.backgroundColor = Color.white;

        so.ApplyModifiedProperties();
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!isPainting)
            return;

        Event e = Event.current;

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 10000f, placementLayer))
        {
            Handles.color = Color.green;
            Handles.DrawWireDisc(hit.point, hit.normal, 1f);

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                SpawnPrefab(hit.point);
                e.Use();
            }
        }

        sceneView.Repaint();
    }

    private void SpawnPrefab(Vector3 position)
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("Prefab array is empty.");
            return;
        }

        GameObject prefab = GetPrefab();

        if (prefab == null)
            return;

        Vector3 spawnPosition = position + Vector3.up * yOffset;

        Quaternion rotation = Quaternion.identity;

        if (randomRotationY)
        {
            rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }

        GameObject spawned = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(spawned, "Spawn Prefab");

        spawned.transform.position = spawnPosition;
        spawned.transform.rotation = rotation;
    }

    private GameObject GetPrefab()
    {
        if (randomPrefab)
        {
            int index = Random.Range(0, prefabs.Length);
            return prefabs[index];
        }

        selectedPrefabIndex = Mathf.Clamp(selectedPrefabIndex, 0, prefabs.Length - 1);
        return prefabs[selectedPrefabIndex];
    }
}