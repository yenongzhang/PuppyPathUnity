using System.Collections.Generic;
using UnityEngine;

public static class VenuePathfinder
{
    private const int DirectSegmentSampleCount = 24;
    private const int ConnectorSegmentSampleCount = 16;

    public static bool TryFindWorldPath(
        VenueMapDefinition mapDefinition,
        Vector2 startPixel,
        Vector2 endPixel,
        List<Vector3> worldPath)
    {
        if (worldPath == null)
            return false;

        worldPath.Clear();

        if (!TryFindMapPath(mapDefinition, startPixel, endPixel, out List<Vector2> mapPath))
            return false;

        foreach (Vector2 mapPoint in mapPath)
            worldPath.Add(mapDefinition.MapPixelToWorld(mapPoint));

        return true;
    }

    public static bool TryFindMapPath(
        VenueMapDefinition mapDefinition,
        Vector2 startPixel,
        Vector2 endPixel,
        out List<Vector2> mapPath)
    {
        mapPath = new List<Vector2>();

        if (mapDefinition == null || !mapDefinition.IsScaleReady())
            return false;

        if (!mapDefinition.IsMapPixelWalkable(startPixel) || !mapDefinition.IsMapPixelWalkable(endPixel))
            return false;

        if (mapDefinition.IsMapSegmentWalkable(startPixel, endPixel, DirectSegmentSampleCount))
        {
            mapPath.Add(startPixel);
            mapPath.Add(endPixel);
            return true;
        }

        VenueNavGraphDefinition graph = mapDefinition.navGraph;
        if (graph == null || graph.nodes == null || graph.nodes.Count == 0)
            return false;

        int startNode = FindNearestVisibleNode(mapDefinition, startPixel);
        int endNode = FindNearestVisibleNode(mapDefinition, endPixel);
        if (startNode < 0 || endNode < 0)
            return false;

        if (!TryFindNodePath(mapDefinition, startNode, endNode, out List<int> nodePath))
            return false;

        mapPath.Add(startPixel);
        foreach (int nodeIndex in nodePath)
            mapPath.Add(graph.nodes[nodeIndex].mapPixel);
        mapPath.Add(endPixel);

        return true;
    }

    private static int FindNearestVisibleNode(VenueMapDefinition mapDefinition, Vector2 mapPixel)
    {
        VenueNavGraphDefinition graph = mapDefinition.navGraph;
        int nearestIndex = -1;
        float nearestSqrDistance = float.PositiveInfinity;

        for (int i = 0; i < graph.nodes.Count; i++)
        {
            VenueNavNodeDefinition node = graph.nodes[i];
            if (node == null || !mapDefinition.IsMapPixelWalkable(node.mapPixel))
                continue;

            if (!mapDefinition.IsMapSegmentWalkable(mapPixel, node.mapPixel, ConnectorSegmentSampleCount))
                continue;

            float sqrDistance = (node.mapPixel - mapPixel).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    private static bool TryFindNodePath(
        VenueMapDefinition mapDefinition,
        int startNodeIndex,
        int endNodeIndex,
        out List<int> nodePath)
    {
        nodePath = new List<int>();

        VenueNavGraphDefinition graph = mapDefinition.navGraph;
        int nodeCount = graph.nodes.Count;
        float[] costs = new float[nodeCount];
        int[] previous = new int[nodeCount];
        bool[] visited = new bool[nodeCount];

        for (int i = 0; i < nodeCount; i++)
        {
            costs[i] = float.PositiveInfinity;
            previous[i] = -1;
        }

        costs[startNodeIndex] = 0f;

        while (true)
        {
            int currentIndex = FindLowestCostUnvisited(costs, visited);
            if (currentIndex < 0)
                break;

            if (currentIndex == endNodeIndex)
                break;

            visited[currentIndex] = true;
            VenueNavNodeDefinition currentNode = graph.nodes[currentIndex];
            if (currentNode == null || currentNode.neighborNodeIds == null)
                continue;

            foreach (string neighborId in currentNode.neighborNodeIds)
            {
                int neighborIndex = FindNodeIndex(graph, neighborId);
                if (neighborIndex < 0 || visited[neighborIndex])
                    continue;

                VenueNavNodeDefinition neighborNode = graph.nodes[neighborIndex];
                if (neighborNode == null)
                    continue;

                if (!mapDefinition.IsMapSegmentWalkable(currentNode.mapPixel, neighborNode.mapPixel))
                    continue;

                float edgeCost = Vector2.Distance(currentNode.mapPixel, neighborNode.mapPixel);
                float candidateCost = costs[currentIndex] + edgeCost;
                if (candidateCost < costs[neighborIndex])
                {
                    costs[neighborIndex] = candidateCost;
                    previous[neighborIndex] = currentIndex;
                }
            }
        }

        if (float.IsPositiveInfinity(costs[endNodeIndex]))
            return false;

        int pathIndex = endNodeIndex;
        while (pathIndex >= 0)
        {
            nodePath.Add(pathIndex);
            pathIndex = previous[pathIndex];
        }

        nodePath.Reverse();
        return true;
    }

    private static int FindLowestCostUnvisited(float[] costs, bool[] visited)
    {
        int bestIndex = -1;
        float bestCost = float.PositiveInfinity;

        for (int i = 0; i < costs.Length; i++)
        {
            if (!visited[i] && costs[i] < bestCost)
            {
                bestCost = costs[i];
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private static int FindNodeIndex(VenueNavGraphDefinition graph, string nodeId)
    {
        if (graph == null || graph.nodes == null || string.IsNullOrEmpty(nodeId))
            return -1;

        for (int i = 0; i < graph.nodes.Count; i++)
        {
            VenueNavNodeDefinition node = graph.nodes[i];
            if (node != null && node.id == nodeId)
                return i;
        }

        return -1;
    }
}
