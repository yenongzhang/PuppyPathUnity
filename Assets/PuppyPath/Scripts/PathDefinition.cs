using UnityEngine;
using System.Collections.Generic;

public class PathDefinition : MonoBehaviour
{
    public string pathId;
    public List<Transform> waypoints = new List<Transform>();

    private void OnValidate()
    {
        RefreshWaypoints();
    }

    [ContextMenu("Refresh Waypoints")]
    public void RefreshWaypoints()
    {
        waypoints.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            waypoints.Add(child);
        }
    }

    public List<Vector3> GetLocalPoints()
    {
        List<Vector3> points = new List<Vector3>();

        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] != null)
                points.Add(waypoints[i].localPosition);
        }

        return points;
    }
}