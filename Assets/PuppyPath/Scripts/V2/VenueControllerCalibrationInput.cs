using UnityEngine;
using UnityEngine.Events;

public class VenueControllerCalibrationInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VenueAlignmentManager alignmentManager;
    [SerializeField] private VenueSpatialAnchorBootstrap spatialAnchorBootstrap;
    [SerializeField] private UIBootSequence startupBootSequence;

    [Header("Input")]
    [Tooltip("Quest's system Meta/Oculus button may be reserved by the OS. Start is the left-controller menu button and is safer for app input.")]
    [SerializeField] private OVRInput.RawButton calibrationButton = OVRInput.RawButton.Start;
    [SerializeField] private bool alsoAcceptAlternateButton = true;
    [SerializeField] private OVRInput.RawButton alternateCalibrationButton = OVRInput.RawButton.Back;
    [SerializeField] private OVRInput.Controller controllerMask = OVRInput.Controller.All;
    [SerializeField] private float holdSeconds = 1.75f;
    [SerializeField] private bool requireHmdMostlyStill = true;
    [SerializeField] private float maxHmdMoveMetersDuringHold = 0.2f;

    [Header("Calibration Action")]
    [SerializeField] private bool resetPositionAndYaw = true;
    [SerializeField] private bool recreateSpatialAnchorAfterCalibration = true;
    [SerializeField] private bool confirmStartupCalibrationAfterCalibration = true;
    [SerializeField] private bool logEvents = true;
    [SerializeField] private UnityEvent onCalibrationTriggered;

    private float holdTimer;
    private Vector3 hmdHoldStartPosition;
    private bool wasHolding;

    private void Update()
    {
        bool isPressed = OVRInput.Get(calibrationButton, controllerMask) ||
            (alsoAcceptAlternateButton && OVRInput.Get(alternateCalibrationButton, controllerMask));
        if (!isPressed)
        {
            ResetHold();
            return;
        }

        if (!wasHolding)
        {
            wasHolding = true;
            holdTimer = 0f;
            hmdHoldStartPosition = GetHmdPosition();
        }

        holdTimer += Time.deltaTime;

        if (requireHmdMostlyStill &&
            Vector3.Distance(GetHmdPosition(), hmdHoldStartPosition) > maxHmdMoveMetersDuringHold)
        {
            if (logEvents)
                Debug.Log("VenueControllerCalibrationInput: calibration hold canceled because HMD moved too far.");

            ResetHold();
            return;
        }

        if (holdTimer >= holdSeconds)
        {
            TriggerCalibration();
            ResetHold();
        }
    }

    [ContextMenu("Trigger Calibration Now")]
    public void TriggerCalibration()
    {
        if (alignmentManager == null)
        {
            Debug.LogWarning("VenueControllerCalibrationInput: missing VenueAlignmentManager.");
            return;
        }

        if (resetPositionAndYaw)
            alignmentManager.AlignVenueOriginToCurrentHeadPose();
        else
            alignmentManager.AlignTranslationOnlyToCurrentHeadPose();

        if (recreateSpatialAnchorAfterCalibration && spatialAnchorBootstrap != null)
            spatialAnchorBootstrap.ReplaceAnchorAtVenueOrigin();

        onCalibrationTriggered?.Invoke();

        if (confirmStartupCalibrationAfterCalibration)
        {
            ResolveStartupBootSequence();
            if (startupBootSequence != null)
                startupBootSequence.ConfirmStartupCalibrationFromCalibration();
        }

        if (logEvents)
            Debug.Log("VenueControllerCalibrationInput: recalibrated VenueOrigin from controller long press.");
    }

    private void ResolveStartupBootSequence()
    {
        if (startupBootSequence != null)
            return;

        startupBootSequence = FindObjectOfType<UIBootSequence>();
    }

    private void ResetHold()
    {
        wasHolding = false;
        holdTimer = 0f;
    }

    private Vector3 GetHmdPosition()
    {
        Transform xrCamera = alignmentManager != null ? alignmentManager.XrCamera : null;
        return xrCamera != null ? xrCamera.position : Vector3.zero;
    }
}
