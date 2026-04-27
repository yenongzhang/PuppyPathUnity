using UnityEngine;
using System.Collections.Generic;

public class DogNavStateTester : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DogGuideController dogGuideController;
    [SerializeField] private Transform fakeUserCamera;

    [Header("Fake Path")]
    [SerializeField] private Transform waypointA;
    [SerializeField] private Transform waypointB;

    private readonly List<Transform> fakePath = new List<Transform>();

    private void Start()
    {
        fakePath.Clear();

        if (waypointA != null)
            fakePath.Add(waypointA);

        if (waypointB != null)
            fakePath.Add(waypointB);

        if (dogGuideController != null && fakeUserCamera != null && fakePath.Count >= 2)
        {
            dogGuideController.BeginGuiding(fakePath, fakeUserCamera);
        }
        else
        {
            Debug.LogWarning("DogNavStateTester: Missing dogGuideController / fakeUserCamera / waypoints.");
        }
    }

    private void Update()
    {
        if (dogGuideController == null)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("Test NavState: Neutral");

            dogGuideController.ApplyNavigationState(
                NavigationRuntimeController.NavState.Neutral,
                3.0f,
                Vector3.forward
            );
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("Test NavState: GettingCloser");

            dogGuideController.ApplyNavigationState(
                NavigationRuntimeController.NavState.GettingCloser,
                2.0f,
                Vector3.forward
            );
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log("Test NavState: GettingFarther");

            dogGuideController.ApplyNavigationState(
                NavigationRuntimeController.NavState.GettingFarther,
                2.5f,
                Vector3.forward
            );
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Debug.Log("Test NavState: Lost");

            dogGuideController.ApplyNavigationState(
                NavigationRuntimeController.NavState.Lost,
                2.5f,
                Vector3.forward
            );
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Debug.Log("Test NavState: Arrived by state");

            dogGuideController.ApplyNavigationState(
                NavigationRuntimeController.NavState.Arrived,
                0.8f,
                Vector3.forward
            );
        }

        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            Debug.Log("Test NavState: Arrived by distance < 0.5");

            dogGuideController.ApplyNavigationState(
                NavigationRuntimeController.NavState.GettingCloser,
                0.4f,
                Vector3.forward
            );
        }
    }
}