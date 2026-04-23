using UnityEngine;
using System.Collections.Generic;

public class DogDummyFollower : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1.0f;
    [SerializeField] private float rotateSpeed = 6f;
    [SerializeField] private float reachDistance = 0.15f;
    [SerializeField] private float stopAtEnd = 0.2f;

    private readonly List<Transform> runtimeWaypoints = new List<Transform>();
    private int currentIndex;
    private bool isMoving;

    public void SetPath(List<Transform> waypoints)
    {
        runtimeWaypoints.Clear();

        if (waypoints != null)
        {
            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] != null)
                    runtimeWaypoints.Add(waypoints[i]);
            }
        }

        currentIndex = 0;
        isMoving = runtimeWaypoints.Count > 0;

        if (isMoving)
        {
            Vector3 startPos = runtimeWaypoints[0].position;
            transform.position = startPos;
        }
    }

    private void Update()
    {
        if (!isMoving) return;
        if (currentIndex >= runtimeWaypoints.Count)
        {
            isMoving = false;
            return;
        }

        Transform target = runtimeWaypoints[currentIndex];
        if (target == null)
        {
            currentIndex++;
            return;
        }

        Vector3 targetPos = target.position;
        Vector3 flatDirection = targetPos - transform.position;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }

        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        float distance = Vector3.Distance(transform.position, targetPos);
        if (distance <= reachDistance)
        {
            currentIndex++;

            if (currentIndex >= runtimeWaypoints.Count)
            {
                isMoving = false;
            }
        }
    }

    public bool IsMoving()
    {
        return isMoving;
    }
}