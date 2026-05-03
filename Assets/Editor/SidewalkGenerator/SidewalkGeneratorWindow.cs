using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SidewalkGeneratorWindow : EditorWindow
{
    private const string DefaultParentName = "Generated_Sidewalks";
    private const string DefaultSidewalkPrefabPath = "Assets/AnimeSuburbCity/Prefabs/Road/SM_Sidewalk02.prefab";
    private const string DefaultSidewalkEndPrefabPath = "Assets/AnimeSuburbCity/Prefabs/Road/SM_Sidewalk02_end.prefab";

    [SerializeField] private Texture2D mapTexture;
    [SerializeField] private Color roadColor = new Color32(255, 155, 0, 255);
    [SerializeField] private float colorTolerance = 35f;
    [SerializeField] private float pixelToWorldScale = 0.1f;
    [SerializeField] private int sampleStep = 4;
    [SerializeField] private float sidewalkSpacing = 4f;
    [SerializeField] private float sidewalkOffsetFromRoad = 1f;
    [SerializeField] private float minSegmentLength = 3f;
    [SerializeField] private string parentName = DefaultParentName;
    [SerializeField] private GameObject sidewalkPrefab;
    [SerializeField] private GameObject sidewalkEndPrefab;
    [SerializeField] private bool previewEdges = true;

    private EdgeBuildResult previewResult;
    private bool previewDirty = true;
    private Vector2 scrollPosition;
    private string lastErrorMessage;

    [MenuItem("Tools/Sidewalk Generator")]
    public static void Open()
    {
        GetWindow<SidewalkGeneratorWindow>("Sidewalk Generator");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        TryAutoAssignPrefabs();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        try
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Map Source", EditorStyles.boldLabel);
            mapTexture = (Texture2D)EditorGUILayout.ObjectField("Map Texture", mapTexture, typeof(Texture2D), false);
            roadColor = EditorGUILayout.ColorField("Road Color", roadColor);
            colorTolerance = EditorGUILayout.Slider("Color Tolerance", colorTolerance, 0f, 255f);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Placement", EditorStyles.boldLabel);
            pixelToWorldScale = EditorGUILayout.FloatField("Pixel To World Scale", pixelToWorldScale);
            sampleStep = EditorGUILayout.IntSlider("Sample Step", sampleStep, 1, 32);
            sidewalkSpacing = EditorGUILayout.FloatField("Sidewalk Spacing", sidewalkSpacing);
            sidewalkOffsetFromRoad = EditorGUILayout.FloatField("Sidewalk Offset From Road", sidewalkOffsetFromRoad);
            minSegmentLength = EditorGUILayout.FloatField("Min Segment Length", minSegmentLength);
            parentName = EditorGUILayout.TextField("Parent Name", parentName);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);
            sidewalkPrefab = (GameObject)EditorGUILayout.ObjectField("Sidewalk Prefab", sidewalkPrefab, typeof(GameObject), false);
            sidewalkEndPrefab = (GameObject)EditorGUILayout.ObjectField("Sidewalk End Prefab", sidewalkEndPrefab, typeof(GameObject), false);

            EditorGUILayout.Space(6f);
            previewEdges = EditorGUILayout.Toggle("Preview/Gizmos", previewEdges);

            if (EditorGUI.EndChangeCheck())
            {
                ClampSettings();
                previewDirty = true;
                lastErrorMessage = null;
                SceneView.RepaintAll();
            }

            DrawWarnings();
            DrawPreviewStats();

            EditorGUILayout.Space(8f);

            if (GUILayout.Button("Refresh Preview", GUILayout.Height(26f)))
            {
                RebuildPreview();
            }

            EditorGUILayout.Space(4f);

            using (new EditorGUI.DisabledScope(!CanGenerate()))
            {
                if (GUILayout.Button("Generate Sidewalks", GUILayout.Height(32f)))
                {
                    GenerateSidewalks();
                }
            }

            if (GUILayout.Button("Clear Generated Sidewalks", GUILayout.Height(26f)))
            {
                ClearGeneratedSidewalks(true);
            }
        }
        finally
        {
            EditorGUILayout.EndScrollView();
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!previewEdges || previewResult == null || previewResult.Polylines == null)
        {
            return;
        }

        for (int i = 0; i < previewResult.Polylines.Count; i++)
        {
            EdgePolyline polyline = previewResult.Polylines[i];
            if (polyline.WorldPoints == null || polyline.WorldPoints.Length < 2)
            {
                continue;
            }

            Handles.color = new Color(0f, 0.75f, 1f, 0.45f);
            Handles.DrawAAPolyLine(2f, polyline.WorldPoints);

            if (polyline.OffsetWorldPoints != null && polyline.OffsetWorldPoints.Length >= 2)
            {
                Handles.color = new Color(1f, 0.75f, 0f, 0.95f);
                Handles.DrawAAPolyLine(4f, polyline.OffsetWorldPoints);

                if (!polyline.Closed)
                {
                    DrawEndpoint(polyline.OffsetWorldPoints[0]);
                    DrawEndpoint(polyline.OffsetWorldPoints[polyline.OffsetWorldPoints.Length - 1]);
                }
            }
        }
    }

    private void DrawEndpoint(Vector3 position)
    {
        float size = HandleUtility.GetHandleSize(position) * 0.05f;
        Handles.color = new Color(1f, 0.25f, 0.1f, 1f);
        Handles.DrawSolidDisc(position, Vector3.up, size);
    }

    private void DrawWarnings()
    {
        if (mapTexture == null)
        {
            EditorGUILayout.HelpBox("Assign a map Texture2D first.", MessageType.Info);
        }
        else if (!mapTexture.isReadable)
        {
            EditorGUILayout.HelpBox("Texture is not Read/Write enabled. The tool will read it through a temporary RenderTexture copy.", MessageType.None);
        }

        if (sidewalkPrefab == null || sidewalkEndPrefab == null)
        {
            EditorGUILayout.HelpBox("Assign SM_Sidewalk02 and SM_Sidewalk02_end prefabs before generation.", MessageType.Warning);
        }
        else if (!IsPrefabAsset(sidewalkPrefab) || !IsPrefabAsset(sidewalkEndPrefab))
        {
            EditorGUILayout.HelpBox("Use prefab assets from the Project window, not scene instances. The tool instantiates through PrefabUtility.", MessageType.Warning);
        }

        if (previewDirty && previewEdges && mapTexture != null)
        {
            EditorGUILayout.HelpBox("Preview is out of date. Press Refresh Preview to rebuild edge gizmos.", MessageType.None);
        }

        if (!string.IsNullOrEmpty(lastErrorMessage))
        {
            EditorGUILayout.HelpBox(lastErrorMessage, MessageType.Error);
        }
    }

    private void DrawPreviewStats()
    {
        if (previewResult == null)
        {
            return;
        }

        EditorGUILayout.HelpBox(
            string.Format(
                "Preview: {0} road samples, {1} contour segments, {2} usable edge lines, {3:0.##} world units.",
                previewResult.RoadSampleCount,
                previewResult.ContourSegmentCount,
                previewResult.Polylines.Count,
                previewResult.TotalLength),
            MessageType.None);
    }

    private bool CanGenerate()
    {
        return mapTexture != null
            && sidewalkPrefab != null
            && sidewalkEndPrefab != null
            && IsPrefabAsset(sidewalkPrefab)
            && IsPrefabAsset(sidewalkEndPrefab);
    }

    private void TryAutoAssignPrefabs()
    {
        if (sidewalkPrefab == null)
        {
            sidewalkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultSidewalkPrefabPath);
        }

        if (sidewalkEndPrefab == null)
        {
            sidewalkEndPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultSidewalkEndPrefabPath);
        }
    }

    private void ClampSettings()
    {
        colorTolerance = Mathf.Clamp(colorTolerance, 0f, 255f);
        pixelToWorldScale = Mathf.Max(0.001f, pixelToWorldScale);
        sampleStep = Mathf.Max(1, sampleStep);
        sidewalkSpacing = Mathf.Max(0.05f, sidewalkSpacing);
        minSegmentLength = Mathf.Max(0f, minSegmentLength);

        if (string.IsNullOrEmpty(parentName))
        {
            parentName = DefaultParentName;
        }
    }

    private void RebuildPreview()
    {
        ClampSettings();

        if (mapTexture == null)
        {
            previewResult = null;
            previewDirty = false;
            SceneView.RepaintAll();
            return;
        }

        try
        {
            previewResult = BuildEdgeData();
            previewDirty = false;
            lastErrorMessage = null;
            SceneView.RepaintAll();
        }
        catch (Exception exception)
        {
            lastErrorMessage = "Sidewalk preview failed: " + exception.Message;
            Debug.LogError(lastErrorMessage);
        }
    }

    private void GenerateSidewalks()
    {
        ClampSettings();

        if (!CanGenerate())
        {
            EditorUtility.DisplayDialog("Sidewalk Generator", "Assign a readable map texture and both prefab assets first.", "OK");
            return;
        }

        int existingGeneratedObjects = CountGeneratedSidewalkObjects();
        if (existingGeneratedObjects > 0)
        {
            int choice = EditorUtility.DisplayDialogComplex(
                "Generated sidewalks already exist",
                "Found " + existingGeneratedObjects + " generated sidewalk objects. Clear them before generating new sidewalks?",
                "Clear and Generate",
                "Generate Without Clearing",
                "Cancel");

            if (choice == 2)
            {
                return;
            }

            if (choice == 0)
            {
                ClearGeneratedSidewalks(false);
            }
        }

        EdgeBuildResult edgeData;
        try
        {
            edgeData = BuildEdgeData();
        }
        catch (Exception exception)
        {
            lastErrorMessage = "Failed to read or process texture: " + exception.Message;
            EditorUtility.DisplayDialog("Sidewalk Generator", "Failed to read or process texture: " + exception.Message, "OK");
            return;
        }

        if (edgeData.Polylines.Count == 0)
        {
            EditorUtility.DisplayDialog("Sidewalk Generator", "No road edges were found with the current color and tolerance.", "OK");
            previewResult = edgeData;
            previewDirty = false;
            SceneView.RepaintAll();
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Generate Sidewalks");

        GameObject parent = GetOrCreateGeneratedParent();
        int createdStraight = 0;
        int createdEnds = 0;

        for (int i = 0; i < edgeData.Polylines.Count; i++)
        {
            GeneratePolyline(edgeData, edgeData.Polylines[i], parent.transform, ref createdStraight, ref createdEnds);
        }

        Undo.CollapseUndoOperations(undoGroup);

        Selection.activeGameObject = parent;
        EditorSceneManager.MarkSceneDirty(parent.scene);

        previewResult = edgeData;
        previewDirty = false;
        lastErrorMessage = null;
        SceneView.RepaintAll();

        Debug.Log("Sidewalk Generator created " + createdStraight + " sidewalk objects and " + createdEnds + " end objects.");
    }

    private void GeneratePolyline(EdgeBuildResult edgeData, EdgePolyline polyline, Transform parent, ref int createdStraight, ref int createdEnds)
    {
        if (polyline.Length < minSegmentLength)
        {
            return;
        }

        float spacing = Mathf.Max(0.05f, sidewalkSpacing);
        int lineStraightCount = 0;

        if (polyline.Closed)
        {
            for (float distance = 0f; distance < polyline.Length; distance += spacing)
            {
                if (CreateSidewalkInstance(edgeData, polyline, distance, false, sidewalkPrefab, parent))
                {
                    createdStraight++;
                    lineStraightCount++;
                }
            }

            return;
        }

        for (float distance = spacing * 0.5f; distance <= polyline.Length - spacing * 0.5f; distance += spacing)
        {
            if (CreateSidewalkInstance(edgeData, polyline, distance, false, sidewalkPrefab, parent))
            {
                createdStraight++;
                lineStraightCount++;
            }
        }

        if (lineStraightCount == 0 && polyline.Length >= minSegmentLength)
        {
            if (CreateSidewalkInstance(edgeData, polyline, polyline.Length * 0.5f, false, sidewalkPrefab, parent))
            {
                createdStraight++;
                lineStraightCount++;
            }
        }

        if (CreateSidewalkInstance(edgeData, polyline, 0f, true, sidewalkEndPrefab, parent))
        {
            createdEnds++;
        }

        if (CreateSidewalkInstance(edgeData, polyline, polyline.Length, false, sidewalkEndPrefab, parent))
        {
            createdEnds++;
        }
    }

    private bool CreateSidewalkInstance(EdgeBuildResult edgeData, EdgePolyline polyline, float distance, bool reverseTangent, GameObject prefab, Transform parent)
    {
        Vector2 gridPoint;
        Vector2 tangent;

        if (!EvaluateAtDistance(polyline, distance, out gridPoint, out tangent))
        {
            return false;
        }

        if (reverseTangent)
        {
            tangent = -tangent;
        }

        Vector2 normal = FindOutwardNormal(gridPoint, tangent, edgeData.RoadMask);
        Vector3 position = GridToWorld(gridPoint) + new Vector3(normal.x, 0f, normal.y) * sidewalkOffsetFromRoad;
        Vector3 forward = new Vector3(tangent.x, 0f, tangent.y);

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }

        Quaternion rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        if (instance == null)
        {
            Debug.LogWarning("Could not instantiate prefab: " + prefab.name);
            return false;
        }

        Undo.RegisterCreatedObjectUndo(instance, "Generate Sidewalk");
        Undo.SetTransformParent(instance.transform, parent, "Parent Generated Sidewalk");
        instance.transform.SetPositionAndRotation(position, rotation);

        SidewalkMarker marker = instance.GetComponent<SidewalkMarker>();
        if (marker == null)
        {
            marker = Undo.AddComponent<SidewalkMarker>(instance);
        }

        marker.Configure(false);
        EditorUtility.SetDirty(marker);

        return true;
    }

    private void ClearGeneratedSidewalks(bool askConfirmation)
    {
        List<SidewalkMarker> generatedObjects = new List<SidewalkMarker>();
        List<SidewalkMarker> generatedContainers = new List<SidewalkMarker>();

        SidewalkMarker[] markers = Resources.FindObjectsOfTypeAll<SidewalkMarker>();
        for (int i = 0; i < markers.Length; i++)
        {
            SidewalkMarker marker = markers[i];
            if (marker == null || !marker.IsSidewalkGeneratorMarker || !IsSceneObject(marker.gameObject))
            {
                continue;
            }

            if (marker.IsContainer)
            {
                generatedContainers.Add(marker);
            }
            else
            {
                generatedObjects.Add(marker);
            }
        }

        if (generatedObjects.Count == 0 && generatedContainers.Count == 0)
        {
            if (askConfirmation)
            {
                EditorUtility.DisplayDialog("Sidewalk Generator", "No generated sidewalk objects were found.", "OK");
            }

            return;
        }

        if (askConfirmation)
        {
            bool clear = EditorUtility.DisplayDialog(
                "Clear Generated Sidewalks",
                "Delete " + generatedObjects.Count + " generated sidewalk objects? Manually placed objects without SidewalkMarker will not be touched.",
                "Clear",
                "Cancel");

            if (!clear)
            {
                return;
            }
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Clear Generated Sidewalks");

        for (int i = 0; i < generatedObjects.Count; i++)
        {
            if (generatedObjects[i] != null)
            {
                Undo.DestroyObjectImmediate(generatedObjects[i].gameObject);
            }
        }

        for (int i = 0; i < generatedContainers.Count; i++)
        {
            SidewalkMarker container = generatedContainers[i];
            if (container != null && container.transform.childCount == 0)
            {
                Undo.DestroyObjectImmediate(container.gameObject);
            }
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        SceneView.RepaintAll();
    }

    private int CountGeneratedSidewalkObjects()
    {
        int count = 0;
        SidewalkMarker[] markers = Resources.FindObjectsOfTypeAll<SidewalkMarker>();

        for (int i = 0; i < markers.Length; i++)
        {
            SidewalkMarker marker = markers[i];
            if (marker != null
                && marker.IsSidewalkGeneratorMarker
                && !marker.IsContainer
                && IsSceneObject(marker.gameObject))
            {
                count++;
            }
        }

        return count;
    }

    private GameObject GetOrCreateGeneratedParent()
    {
        SidewalkMarker[] markers = Resources.FindObjectsOfTypeAll<SidewalkMarker>();
        for (int i = 0; i < markers.Length; i++)
        {
            SidewalkMarker marker = markers[i];
            if (marker != null
                && marker.IsSidewalkGeneratorMarker
                && marker.IsContainer
                && IsSceneObject(marker.gameObject)
                && marker.gameObject.name == parentName)
            {
                return marker.gameObject;
            }
        }

        GameObject parent = new GameObject(parentName);
        Undo.RegisterCreatedObjectUndo(parent, "Create Generated Sidewalks Parent");

        SidewalkMarker parentMarker = Undo.AddComponent<SidewalkMarker>(parent);
        parentMarker.Configure(true);
        EditorUtility.SetDirty(parentMarker);

        return parent;
    }

    private EdgeBuildResult BuildEdgeData()
    {
        Color32[] pixels = ReadTexturePixels(mapTexture);

        int sampledWidth;
        int sampledHeight;
        int roadSampleCount;
        bool[,] roadMask = BuildRoadMask(pixels, mapTexture.width, mapTexture.height, out sampledWidth, out sampledHeight, out roadSampleCount);
        List<ContourSegment> contourSegments = BuildContourSegments(roadMask, sampledWidth, sampledHeight);
        List<List<GridPoint>> tracedLines = TracePolylines(contourSegments);

        EdgeBuildResult result = new EdgeBuildResult();
        result.RoadMask = roadMask;
        result.SampledWidth = sampledWidth;
        result.SampledHeight = sampledHeight;
        result.RoadSampleCount = roadSampleCount;
        result.ContourSegmentCount = contourSegments.Count;
        result.Polylines = new List<EdgePolyline>();

        for (int i = 0; i < tracedLines.Count; i++)
        {
            EdgePolyline polyline = CreateEdgePolyline(tracedLines[i], roadMask);
            if (polyline.GridPoints.Count < 2 || polyline.Length < minSegmentLength)
            {
                continue;
            }

            result.TotalLength += polyline.Length;
            result.Polylines.Add(polyline);
        }

        return result;
    }

    private bool[,] BuildRoadMask(Color32[] pixels, int textureWidth, int textureHeight, out int sampledWidth, out int sampledHeight, out int roadSampleCount)
    {
        sampledWidth = Mathf.Max(2, Mathf.CeilToInt(textureWidth / (float)sampleStep) + 1);
        sampledHeight = Mathf.Max(2, Mathf.CeilToInt(textureHeight / (float)sampleStep) + 1);
        roadSampleCount = 0;

        bool[,] mask = new bool[sampledWidth, sampledHeight];
        Color32 target = roadColor;
        float toleranceSqr = colorTolerance * colorTolerance;

        for (int y = 0; y < sampledHeight; y++)
        {
            int sourceY = Mathf.Min(y * sampleStep, textureHeight - 1);

            for (int x = 0; x < sampledWidth; x++)
            {
                int sourceX = Mathf.Min(x * sampleStep, textureWidth - 1);
                Color32 pixel = pixels[sourceY * textureWidth + sourceX];

                // Road detection is configured here: the road mask includes sampled pixels
                // whose RGB distance from roadColor is inside colorTolerance.
                bool isRoad = ColorDistanceSqr(pixel, target) <= toleranceSqr;
                mask[x, y] = isRoad;

                if (isRoad)
                {
                    roadSampleCount++;
                }
            }
        }

        return mask;
    }

    private List<ContourSegment> BuildContourSegments(bool[,] roadMask, int sampledWidth, int sampledHeight)
    {
        List<ContourSegment> segments = new List<ContourSegment>();

        for (int y = 0; y < sampledHeight - 1; y++)
        {
            for (int x = 0; x < sampledWidth - 1; x++)
            {
                int caseIndex = 0;

                if (roadMask[x, y])
                {
                    caseIndex |= 1;
                }

                if (roadMask[x + 1, y])
                {
                    caseIndex |= 2;
                }

                if (roadMask[x + 1, y + 1])
                {
                    caseIndex |= 4;
                }

                if (roadMask[x, y + 1])
                {
                    caseIndex |= 8;
                }

                // Edge finding is configured here. Marching squares converts the binary road
                // mask into connected contour segments along the transition from road to non-road.
                AddMarchingSquaresSegments(x, y, caseIndex, segments);
            }
        }

        return segments;
    }

    private static void AddMarchingSquaresSegments(int x, int y, int caseIndex, List<ContourSegment> segments)
    {
        GridPoint left = new GridPoint(x * 2, y * 2 + 1);
        GridPoint right = new GridPoint((x + 1) * 2, y * 2 + 1);
        GridPoint bottom = new GridPoint(x * 2 + 1, y * 2);
        GridPoint top = new GridPoint(x * 2 + 1, (y + 1) * 2);

        switch (caseIndex)
        {
            case 0:
            case 15:
                break;
            case 1:
            case 14:
                segments.Add(new ContourSegment(left, bottom));
                break;
            case 2:
            case 13:
                segments.Add(new ContourSegment(bottom, right));
                break;
            case 3:
            case 12:
                segments.Add(new ContourSegment(left, right));
                break;
            case 4:
            case 11:
                segments.Add(new ContourSegment(right, top));
                break;
            case 5:
                segments.Add(new ContourSegment(left, top));
                segments.Add(new ContourSegment(bottom, right));
                break;
            case 6:
            case 9:
                segments.Add(new ContourSegment(bottom, top));
                break;
            case 7:
            case 8:
                segments.Add(new ContourSegment(left, top));
                break;
            case 10:
                segments.Add(new ContourSegment(left, bottom));
                segments.Add(new ContourSegment(top, right));
                break;
        }
    }

    private List<List<GridPoint>> TracePolylines(List<ContourSegment> segments)
    {
        Dictionary<GridPoint, List<int>> adjacency = new Dictionary<GridPoint, List<int>>();

        for (int i = 0; i < segments.Count; i++)
        {
            AddAdjacency(adjacency, segments[i].A, i);
            AddAdjacency(adjacency, segments[i].B, i);
        }

        bool[] used = new bool[segments.Count];
        List<List<GridPoint>> polylines = new List<List<GridPoint>>();

        for (int i = 0; i < segments.Count; i++)
        {
            if (used[i])
            {
                continue;
            }

            List<GridPoint> points = new List<GridPoint>();
            used[i] = true;
            points.Add(segments[i].A);
            points.Add(segments[i].B);

            ExtendPolyline(points, true, segments, adjacency, used);

            if (!points[0].Equals(points[points.Count - 1]))
            {
                ExtendPolyline(points, false, segments, adjacency, used);
            }

            RemoveConsecutiveDuplicates(points);

            if (points.Count >= 2)
            {
                polylines.Add(points);
            }
        }

        return polylines;
    }

    private static void AddAdjacency(Dictionary<GridPoint, List<int>> adjacency, GridPoint point, int segmentIndex)
    {
        List<int> connectedSegments;
        if (!adjacency.TryGetValue(point, out connectedSegments))
        {
            connectedSegments = new List<int>();
            adjacency.Add(point, connectedSegments);
        }

        connectedSegments.Add(segmentIndex);
    }

    private static void ExtendPolyline(List<GridPoint> points, bool forward, List<ContourSegment> segments, Dictionary<GridPoint, List<int>> adjacency, bool[] used)
    {
        int guard = 0;

        while (guard < segments.Count)
        {
            guard++;

            GridPoint end = forward ? points[points.Count - 1] : points[0];
            bool hasPrevious = points.Count > 1;
            GridPoint previous = forward ? points[points.Count - 2] : points[1];
            int nextSegment = PickNextSegment(end, previous, hasPrevious, segments, adjacency, used);

            if (nextSegment < 0)
            {
                break;
            }

            used[nextSegment] = true;
            GridPoint nextPoint = segments[nextSegment].Other(end);

            if (forward)
            {
                points.Add(nextPoint);

                if (nextPoint.Equals(points[0]))
                {
                    break;
                }
            }
            else
            {
                points.Insert(0, nextPoint);

                if (nextPoint.Equals(points[points.Count - 1]))
                {
                    break;
                }
            }
        }
    }

    private static int PickNextSegment(GridPoint end, GridPoint previous, bool hasPrevious, List<ContourSegment> segments, Dictionary<GridPoint, List<int>> adjacency, bool[] used)
    {
        List<int> candidates;
        if (!adjacency.TryGetValue(end, out candidates))
        {
            return -1;
        }

        int bestSegment = -1;
        float bestScore = float.NegativeInfinity;
        Vector2 incoming = GridPointToVector(end) - GridPointToVector(previous);

        if (incoming.sqrMagnitude > 0.0001f)
        {
            incoming.Normalize();
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            int segmentIndex = candidates[i];
            if (used[segmentIndex])
            {
                continue;
            }

            GridPoint other = segments[segmentIndex].Other(end);
            Vector2 candidateDirection = GridPointToVector(other) - GridPointToVector(end);

            float score = 0f;
            if (hasPrevious && incoming.sqrMagnitude > 0.0001f && candidateDirection.sqrMagnitude > 0.0001f)
            {
                candidateDirection.Normalize();
                score = Vector2.Dot(incoming, candidateDirection);
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestSegment = segmentIndex;
            }
        }

        return bestSegment;
    }

    private EdgePolyline CreateEdgePolyline(List<GridPoint> tracedPoints, bool[,] roadMask)
    {
        EdgePolyline polyline = new EdgePolyline();
        polyline.GridPoints = new List<Vector2>();

        for (int i = 0; i < tracedPoints.Count; i++)
        {
            polyline.GridPoints.Add(GridPointToVector(tracedPoints[i]));
        }

        polyline.Closed = tracedPoints.Count > 2 && tracedPoints[0].Equals(tracedPoints[tracedPoints.Count - 1]);
        polyline.Length = CalculateWorldLength(polyline.GridPoints);
        polyline.WorldPoints = BuildWorldPreviewPoints(polyline.GridPoints, roadMask, false);
        polyline.OffsetWorldPoints = BuildWorldPreviewPoints(polyline.GridPoints, roadMask, true);

        return polyline;
    }

    private Vector3[] BuildWorldPreviewPoints(List<Vector2> gridPoints, bool[,] roadMask, bool offsetFromRoad)
    {
        Vector3[] worldPoints = new Vector3[gridPoints.Count];

        for (int i = 0; i < gridPoints.Count; i++)
        {
            Vector2 gridPoint = gridPoints[i];
            Vector3 worldPoint = GridToWorld(gridPoint);

            if (offsetFromRoad)
            {
                Vector2 tangent = EstimateTangent(gridPoints, i);
                Vector2 normal = FindOutwardNormal(gridPoint, tangent, roadMask);
                worldPoint += new Vector3(normal.x, 0f, normal.y) * sidewalkOffsetFromRoad;
            }

            worldPoints[i] = worldPoint;
        }

        return worldPoints;
    }

    private static void RemoveConsecutiveDuplicates(List<GridPoint> points)
    {
        for (int i = points.Count - 1; i > 0; i--)
        {
            if (points[i].Equals(points[i - 1]))
            {
                points.RemoveAt(i);
            }
        }
    }

    private float CalculateWorldLength(List<Vector2> gridPoints)
    {
        float length = 0f;

        for (int i = 1; i < gridPoints.Count; i++)
        {
            length += Vector2.Distance(gridPoints[i - 1], gridPoints[i]) * sampleStep * pixelToWorldScale;
        }

        return length;
    }

    private bool EvaluateAtDistance(EdgePolyline polyline, float distance, out Vector2 gridPoint, out Vector2 tangent)
    {
        gridPoint = Vector2.zero;
        tangent = Vector2.right;

        if (polyline.GridPoints == null || polyline.GridPoints.Count < 2)
        {
            return false;
        }

        distance = Mathf.Clamp(distance, 0f, polyline.Length);
        float walked = 0f;
        float gridToWorld = sampleStep * pixelToWorldScale;

        for (int i = 1; i < polyline.GridPoints.Count; i++)
        {
            Vector2 a = polyline.GridPoints[i - 1];
            Vector2 b = polyline.GridPoints[i];
            Vector2 segment = b - a;
            float segmentLength = segment.magnitude * gridToWorld;

            if (segmentLength <= 0.0001f)
            {
                continue;
            }

            if (distance <= walked + segmentLength || i == polyline.GridPoints.Count - 1)
            {
                float t = Mathf.Clamp01((distance - walked) / segmentLength);
                gridPoint = Vector2.Lerp(a, b, t);
                tangent = segment.normalized;
                return true;
            }

            walked += segmentLength;
        }

        return false;
    }

    private Vector3 GridToWorld(Vector2 gridPoint)
    {
        float pixelX = gridPoint.x * sampleStep;
        float pixelY = gridPoint.y * sampleStep;
        float worldX = (pixelX - mapTexture.width * 0.5f) * pixelToWorldScale;
        float worldZ = (pixelY - mapTexture.height * 0.5f) * pixelToWorldScale;
        return new Vector3(worldX, 0f, worldZ);
    }

    private static Vector2 EstimateTangent(List<Vector2> gridPoints, int index)
    {
        if (gridPoints.Count < 2)
        {
            return Vector2.right;
        }

        int previousIndex = Mathf.Max(0, index - 1);
        int nextIndex = Mathf.Min(gridPoints.Count - 1, index + 1);

        if (previousIndex == nextIndex)
        {
            nextIndex = Mathf.Min(gridPoints.Count - 1, previousIndex + 1);
        }

        Vector2 tangent = gridPoints[nextIndex] - gridPoints[previousIndex];
        if (tangent.sqrMagnitude < 0.0001f)
        {
            return Vector2.right;
        }

        return tangent.normalized;
    }

    private static Vector2 FindOutwardNormal(Vector2 gridPoint, Vector2 tangent, bool[,] roadMask)
    {
        if (tangent.sqrMagnitude < 0.0001f)
        {
            tangent = Vector2.right;
        }

        tangent.Normalize();
        Vector2 side = new Vector2(tangent.y, -tangent.x);
        const float sampleDistance = 0.75f;

        bool sideIsRoad = IsRoadAt(roadMask, gridPoint + side * sampleDistance);
        bool oppositeIsRoad = IsRoadAt(roadMask, gridPoint - side * sampleDistance);

        if (sideIsRoad != oppositeIsRoad)
        {
            return sideIsRoad ? -side : side;
        }

        Vector2 inward = new Vector2(
            MaskValue(roadMask, gridPoint + Vector2.right * sampleDistance) - MaskValue(roadMask, gridPoint - Vector2.right * sampleDistance),
            MaskValue(roadMask, gridPoint + Vector2.up * sampleDistance) - MaskValue(roadMask, gridPoint - Vector2.up * sampleDistance));

        if (inward.sqrMagnitude > 0.0001f)
        {
            return -inward.normalized;
        }

        return side;
    }

    private static bool IsRoadAt(bool[,] roadMask, Vector2 gridPoint)
    {
        int x = Mathf.RoundToInt(gridPoint.x);
        int y = Mathf.RoundToInt(gridPoint.y);

        if (x < 0 || y < 0 || x >= roadMask.GetLength(0) || y >= roadMask.GetLength(1))
        {
            return false;
        }

        return roadMask[x, y];
    }

    private static float MaskValue(bool[,] roadMask, Vector2 gridPoint)
    {
        return IsRoadAt(roadMask, gridPoint) ? 1f : 0f;
    }

    private static float ColorDistanceSqr(Color32 a, Color32 b)
    {
        int dr = a.r - b.r;
        int dg = a.g - b.g;
        int db = a.b - b.b;
        return dr * dr + dg * dg + db * db;
    }

    private static Vector2 GridPointToVector(GridPoint point)
    {
        return new Vector2(point.X2 * 0.5f, point.Y2 * 0.5f);
    }

    private static Color32[] ReadTexturePixels(Texture2D texture)
    {
        if (texture == null)
        {
            throw new ArgumentNullException("texture");
        }

        if (texture.isReadable)
        {
            try
            {
                return texture.GetPixels32();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Direct texture read failed, trying RenderTexture copy: " + exception.Message);
            }
        }

        return ReadTexturePixelsThroughRenderTexture(texture);
    }

    private static Color32[] ReadTexturePixelsThroughRenderTexture(Texture2D texture)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture temporary = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        Texture2D readableCopy = null;

        try
        {
            Graphics.Blit(texture, temporary);
            RenderTexture.active = temporary;

            readableCopy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            readableCopy.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            readableCopy.Apply(false, false);

            return readableCopy.GetPixels32();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                "Texture could not be read. Enable Read/Write in the texture import settings or use a valid Texture2D asset. " + exception.Message,
                exception);
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporary);

            if (readableCopy != null)
            {
                UnityEngine.Object.DestroyImmediate(readableCopy);
            }
        }
    }

    private static bool IsPrefabAsset(GameObject prefab)
    {
        return prefab != null
            && AssetDatabase.Contains(prefab)
            && PrefabUtility.GetPrefabAssetType(prefab) != PrefabAssetType.NotAPrefab;
    }

    private static bool IsSceneObject(GameObject gameObject)
    {
        return gameObject != null
            && gameObject.scene.IsValid()
            && !EditorUtility.IsPersistent(gameObject);
    }

    private sealed class EdgeBuildResult
    {
        public bool[,] RoadMask;
        public int SampledWidth;
        public int SampledHeight;
        public int RoadSampleCount;
        public int ContourSegmentCount;
        public float TotalLength;
        public List<EdgePolyline> Polylines;
    }

    private sealed class EdgePolyline
    {
        public List<Vector2> GridPoints;
        public Vector3[] WorldPoints;
        public Vector3[] OffsetWorldPoints;
        public float Length;
        public bool Closed;
    }

    private struct ContourSegment
    {
        public readonly GridPoint A;
        public readonly GridPoint B;

        public ContourSegment(GridPoint a, GridPoint b)
        {
            A = a;
            B = b;
        }

        public GridPoint Other(GridPoint point)
        {
            return point.Equals(A) ? B : A;
        }
    }

    private struct GridPoint
    {
        public readonly int X2;
        public readonly int Y2;

        public GridPoint(int x2, int y2)
        {
            X2 = x2;
            Y2 = y2;
        }

        public bool Equals(GridPoint other)
        {
            return X2 == other.X2 && Y2 == other.Y2;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is GridPoint))
            {
                return false;
            }

            return Equals((GridPoint)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X2 * 397) ^ Y2;
            }
        }
    }
}
