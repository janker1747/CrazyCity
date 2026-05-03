using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class SidewalkPainter : EditorWindow
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private LayerMask placementLayer = ~0;
    [SerializeField] private float yOffset = 0f;

    private bool isPainting;
    private HashSet<Vector3> paintedPositions = new HashSet<Vector3>();

    [MenuItem("Tools/Sidewalk Painter")]
    public static void Open()
    {
        GetWindow<SidewalkPainter>("Sidewalk Painter");
    }

    private void OnGUI()
    {
        prefab = (GameObject)EditorGUILayout.ObjectField(
            "Prefab",
            prefab,
            typeof(GameObject),
            false
        );

        gridSize = EditorGUILayout.FloatField("Grid Size", gridSize);

        placementLayer = EditorGUILayout.MaskField(
            "Placement Layers",
            placementLayer,
            UnityEditorInternal.InternalEditorUtility.layers
        );

        yOffset = EditorGUILayout.FloatField("Y Offset", yOffset);

        GUILayout.Space(10);

        GUI.backgroundColor = isPainting ? Color.green : Color.white;

        if (GUILayout.Button(isPainting ? "Painting ON" : "Painting OFF"))
        {
            isPainting = !isPainting;
            paintedPositions.Clear();
            SceneView.RepaintAll();
        }

        GUI.backgroundColor = Color.white;
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
        if (!isPainting || prefab == null)
            return;

        Event e = Event.current;

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, placementLayer))
        {
            Vector3 snapped = SnapToGrid(hit.point);

            Handles.color = Color.green;
            Handles.DrawWireCube(snapped, Vector3.one * gridSize);

            if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && e.button == 0 && !e.alt)
            {
                if (!paintedPositions.Contains(snapped))
                {
                    Spawn(snapped);
                    paintedPositions.Add(snapped);
                }

                e.Use();
            }
        }

        sceneView.Repaint();
    }

    private Vector3 SnapToGrid(Vector3 pos)
    {
        float x = Mathf.Round(pos.x / gridSize) * gridSize;
        float z = Mathf.Round(pos.z / gridSize) * gridSize;
        return new Vector3(x, pos.y + yOffset, z);
    }

    private void Spawn(Vector3 position)
    {
        GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(obj, "Paint Sidewalk");

        obj.transform.position = position;
        obj.transform.rotation = Quaternion.identity;
    }
}