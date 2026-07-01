#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VenueCalibrationDebugView))]
public class VenueCalibrationDebugViewEditor : Editor
{
    private const float CalibrationHandleSize = 0.18f;
    private const float AttractionHandleSize = 0.16f;
    private const float PolygonHandleSize = 0.11f;
    private const float NavNodeHandleSize = 0.14f;
    private const float NavNodeSelectHandleSize = 0.12f;
    private const float NavNodeSelectOffsetSize = 0.26f;

    private static string selectedNavNodeA;
    private static string selectedNavNodeB;

    private void OnSceneGUI()
    {
        VenueCalibrationDebugView view = (VenueCalibrationDebugView)target;
        VenueMapDefinition map = view.MapDefinition;
        if (map == null || !map.IsScaleReady())
            return;

        if (view.EditCalibrationPointsInScene)
            DrawCalibrationHandles(map);

        if (view.EditAttractionsInScene)
            DrawAttractionHandles(map);

        if (view.EditWalkableAreasInScene)
            DrawWalkableHandles(map);

        if (view.EditObstacleAreasInScene)
            DrawObstacleHandles(map);

        if (view.EditNavGraphInScene)
        {
            DrawNavNodeHandles(map);
            DrawNavGraphEditPanel(map);
        }
    }

    private static void DrawCalibrationHandles(VenueMapDefinition map)
    {
        Handles.color = Color.red;

        Vector3 originWorld = map.originWorldPosition;
        Vector3 movedOriginWorld = DrawFreeMoveHandle(originWorld, CalibrationHandleSize);
        Handles.Label(originWorld + Vector3.up * 0.38f, "mapOriginPixel");

        if (movedOriginWorld != originWorld)
        {
            Undo.RecordObject(map, "Move map origin pixel");
            map.SetMapOriginPixelPreservingWorldLayout(map.WorldToMapPixel(movedOriginWorld));
            EditorUtility.SetDirty(map);
        }

        DrawMapPixelHandle(
            map,
            map.scalePointAPixel,
            "scalePointA",
            "Move scale point A",
            delegate(Vector2 movedPixel) { map.SetScalePointAPixelPreservingMetersPerPixel(movedPixel); });

        DrawMapPixelHandle(
            map,
            map.scalePointBPixel,
            "scalePointB",
            "Move scale point B",
            delegate(Vector2 movedPixel) { map.SetScalePointBPixelPreservingMetersPerPixel(movedPixel); });
    }

    private static void DrawAttractionHandles(VenueMapDefinition map)
    {
        if (map.attractions == null)
            return;

        Handles.color = new Color(1f, 0.85f, 0.1f, 1f);

        for (int i = 0; i < map.attractions.Count; i++)
        {
            AttractionDefinition attraction = map.attractions[i];
            if (attraction == null)
                continue;

            Vector3 world = map.MapPixelToWorld(attraction.collectibleSpawnPixel);
            Vector3 movedWorld = DrawFreeMoveHandle(world, AttractionHandleSize);
            Handles.Label(world + Vector3.up * 0.25f, string.IsNullOrEmpty(attraction.id) ? $"attraction_{i}" : attraction.id);

            if (movedWorld != world)
            {
                Undo.RecordObject(map, "Move attraction spawn pixel");
                attraction.collectibleSpawnPixel = map.WorldToMapPixel(movedWorld);
                if (!attraction.hasCustomArrivalPixel)
                    attraction.arrivalPixel = attraction.collectibleSpawnPixel;
                EditorUtility.SetDirty(map);
            }
        }
    }

    private static void DrawWalkableHandles(VenueMapDefinition map)
    {
        if (map.walkableAreas == null)
            return;

        Handles.color = new Color(0.2f, 0.9f, 0.35f, 1f);

        foreach (WalkableAreaDefinition area in map.walkableAreas)
            DrawPolygonHandles(map, area, "Move walkable polygon point");
    }

    private static void DrawObstacleHandles(VenueMapDefinition map)
    {
        if (map.obstacleAreas == null)
            return;

        Handles.color = new Color(1f, 0.2f, 0.2f, 1f);

        foreach (ObstacleAreaDefinition area in map.obstacleAreas)
            DrawPolygonHandles(map, area, "Move obstacle polygon point");
    }

    private static void DrawPolygonHandles(VenueMapDefinition map, WalkableAreaDefinition area, string undoName)
    {
        if (area == null || area.polygonPixels == null)
            return;

        for (int i = 0; i < area.polygonPixels.Count; i++)
        {
            Vector3 world = map.MapPixelToWorld(area.polygonPixels[i]);
            Vector3 movedWorld = DrawFreeMoveHandle(world, PolygonHandleSize);
            Handles.Label(world + Vector3.up * 0.18f, $"{area.id}[{i}]");

            if (movedWorld != world)
            {
                Undo.RecordObject(map, undoName);
                area.polygonPixels[i] = map.WorldToMapPixel(movedWorld);
                EditorUtility.SetDirty(map);
            }
        }
    }

    private static void DrawNavNodeHandles(VenueMapDefinition map)
    {
        if (map.navGraph == null || map.navGraph.nodes == null)
            return;

        foreach (VenueNavNodeDefinition node in map.navGraph.nodes)
        {
            if (node == null)
                continue;

            bool isSelected = node.id == selectedNavNodeA || node.id == selectedNavNodeB;
            Vector3 world = map.MapPixelToWorld(node.mapPixel);

            Handles.color = isSelected ? Color.yellow : new Color(0.2f, 0.65f, 1f, 1f);
            Vector3 movedWorld = DrawFreeMoveHandle(world, NavNodeHandleSize);
            Handles.Label(world + Vector3.up * 0.22f, node.id);

            if (movedWorld != world)
            {
                Undo.RecordObject(map, "Move nav graph node");
                node.mapPixel = map.WorldToMapPixel(movedWorld);
                EditorUtility.SetDirty(map);
            }

            Vector3 selectWorld = world + GetSceneViewPlanarRight(world) * (HandleUtility.GetHandleSize(world) * NavNodeSelectOffsetSize);
            float selectSize = HandleUtility.GetHandleSize(selectWorld) * NavNodeSelectHandleSize;
            Handles.color = isSelected ? Color.green : Color.cyan;

            if (Handles.Button(
                selectWorld,
                Quaternion.identity,
                selectSize,
                selectSize * 1.35f,
                Handles.DotHandleCap))
            {
                SelectNavNode(node.id);
                SceneView.RepaintAll();
            }
        }
    }

    private static void DrawNavGraphEditPanel(VenueMapDefinition map)
    {
        ClearMissingSelectedNavNodes(map);

        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(12f, 12f, 280f, 150f), "Nav Graph Editing", GUI.skin.window);

        GUILayout.Label("Click two cyan dots beside blue nodes.");
        GUILayout.Label("A: " + (string.IsNullOrEmpty(selectedNavNodeA) ? "(none)" : selectedNavNodeA));
        GUILayout.Label("B: " + (string.IsNullOrEmpty(selectedNavNodeB) ? "(none)" : selectedNavNodeB));

        bool hasPair = !string.IsNullOrEmpty(selectedNavNodeA) && !string.IsNullOrEmpty(selectedNavNodeB);
        EditorGUI.BeginDisabledGroup(!hasPair);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Connect"))
            ConnectSelectedNavNodes(map);

        if (GUILayout.Button("Disconnect"))
            DisconnectSelectedNavNodes(map);
        GUILayout.EndHorizontal();

        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("Clear Selection"))
        {
            selectedNavNodeA = null;
            selectedNavNodeB = null;
            SceneView.RepaintAll();
        }

        GUILayout.EndArea();
        Handles.EndGUI();
    }

    private static void SelectNavNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId))
            return;

        if (selectedNavNodeA == nodeId)
        {
            selectedNavNodeA = null;
            return;
        }

        if (selectedNavNodeB == nodeId)
        {
            selectedNavNodeB = null;
            return;
        }

        if (string.IsNullOrEmpty(selectedNavNodeA))
            selectedNavNodeA = nodeId;
        else if (string.IsNullOrEmpty(selectedNavNodeB))
            selectedNavNodeB = nodeId;
        else
        {
            selectedNavNodeA = selectedNavNodeB;
            selectedNavNodeB = nodeId;
        }
    }

    private static void ConnectSelectedNavNodes(VenueMapDefinition map)
    {
        VenueNavNodeDefinition first = FindSelectedNavNode(map, selectedNavNodeA);
        VenueNavNodeDefinition second = FindSelectedNavNode(map, selectedNavNodeB);
        if (first == null || second == null || first == second)
            return;

        Undo.RecordObject(map, "Connect nav graph nodes");
        AddNeighborIfMissing(first, second.id);
        AddNeighborIfMissing(second, first.id);
        EditorUtility.SetDirty(map);
        SceneView.RepaintAll();
    }

    private static void DisconnectSelectedNavNodes(VenueMapDefinition map)
    {
        VenueNavNodeDefinition first = FindSelectedNavNode(map, selectedNavNodeA);
        VenueNavNodeDefinition second = FindSelectedNavNode(map, selectedNavNodeB);
        if (first == null || second == null || first == second)
            return;

        Undo.RecordObject(map, "Disconnect nav graph nodes");
        RemoveNeighbor(first, second.id);
        RemoveNeighbor(second, first.id);
        EditorUtility.SetDirty(map);
        SceneView.RepaintAll();
    }

    private static void ClearMissingSelectedNavNodes(VenueMapDefinition map)
    {
        if (!string.IsNullOrEmpty(selectedNavNodeA) && FindSelectedNavNode(map, selectedNavNodeA) == null)
            selectedNavNodeA = null;

        if (!string.IsNullOrEmpty(selectedNavNodeB) && FindSelectedNavNode(map, selectedNavNodeB) == null)
            selectedNavNodeB = null;
    }

    private static VenueNavNodeDefinition FindSelectedNavNode(VenueMapDefinition map, string nodeId)
    {
        if (map == null || map.navGraph == null || string.IsNullOrEmpty(nodeId))
            return null;

        return map.navGraph.FindNode(nodeId);
    }

    private static void AddNeighborIfMissing(VenueNavNodeDefinition node, string neighborNodeId)
    {
        if (node.neighborNodeIds == null)
            node.neighborNodeIds = new System.Collections.Generic.List<string>();

        if (!node.neighborNodeIds.Contains(neighborNodeId))
            node.neighborNodeIds.Add(neighborNodeId);
    }

    private static void RemoveNeighbor(VenueNavNodeDefinition node, string neighborNodeId)
    {
        if (node.neighborNodeIds == null)
            return;

        node.neighborNodeIds.RemoveAll(delegate(string id) { return id == neighborNodeId; });
    }

    private static Vector3 GetSceneViewPlanarRight(Vector3 world)
    {
        SceneView sceneView = SceneView.currentDrawingSceneView;
        if (sceneView == null || sceneView.camera == null)
            return Vector3.right;

        Vector3 right = Vector3.ProjectOnPlane(sceneView.camera.transform.right, Vector3.up);
        if (right.sqrMagnitude < 0.0001f)
            right = Vector3.ProjectOnPlane(sceneView.camera.transform.up, Vector3.up);

        if (right.sqrMagnitude < 0.0001f)
            return Vector3.right;

        return right.normalized;
    }

    private delegate void PixelSetter(Vector2 movedPixel);

    private static void DrawMapPixelHandle(
        VenueMapDefinition map,
        Vector2 mapPixel,
        string label,
        string undoName,
        PixelSetter setPixel)
    {
        Vector3 world = map.MapPixelToWorld(mapPixel);
        Vector3 movedWorld = DrawFreeMoveHandle(world, CalibrationHandleSize);
        Handles.Label(world + Vector3.up * 0.32f, label);

        if (movedWorld != world)
        {
            Undo.RecordObject(map, undoName);
            setPixel(map.WorldToMapPixel(movedWorld));
            EditorUtility.SetDirty(map);
        }
    }

    private static Vector3 DrawFreeMoveHandle(Vector3 world, float size)
    {
        float handleSize = HandleUtility.GetHandleSize(world) * size;
        return Handles.FreeMoveHandle(
            world,
            handleSize,
            Vector3.zero,
            Handles.SphereHandleCap);
    }
}
#endif
