using UnityEngine;
using UnityEngine.InputSystem;
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

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (keyboard[Key.Digit1].wasPressedThisFrame)
        {
            Debug.Log("Test NavState: Neutral");

            dogGuideController.ApplyNavigationState(
                NavigationRuntimeController.NavState.Neutral,
                3.0f,
                Vector3.forward
            );
        }

        if (keyboard[Key.Digit2].wasPressedThisFrame)
        {
            Debug.Log("Test NavState: Waiting");

            dogGuideController.ApplyNavigationState(
                NavigationRuntimeController.NavState.Waiting,
                2.0f,
                Vector3.forward
            );
        }

        if (keyboard[Key.Digit3].wasPressedThisFrame)
        {
            Debug.Log("Test NavState: GettingCloser");

            dogGuideController.ApplyNavigationState(
                NavigationRuntimeController.NavState.GettingCloser,
                2.5f,
                Vector3.forward
            );
        }

        if (keyboard[Key.Digit4].wasPressedThisFrame)
        {
            Debug.Log("Test NavState: GettingFarther");

            dogGuideController.ApplyNavigationState(
                NavigationRuntimeController.NavState.GettingFarther,
                2.5f,
                Vector3.forward
            );
        }

        if (keyboard[Key.Digit5].wasPressedThisFrame)
        {
            Debug.Log("Test NavState: Lost");

            dogGuideController.ApplyNavigationState(
                NavigationRuntimeController.NavState.Lost,
                0.8f,
                Vector3.forward
            );
        }

        if (keyboard[Key.Digit6].wasPressedThisFrame)
        {
            Debug.Log("Test NavState: Arrived");

            dogGuideController.ApplyNavigationState(
                NavigationRuntimeController.NavState.Arrived,
                0.4f,
                Vector3.forward
            );
        }

        if (keyboard[Key.H].wasPressedThisFrame)
        {
            Debug.Log("Test: Force Happy animation preview");
            dogGuideController.ForcePlayHappyPreview();
        }
    }
}