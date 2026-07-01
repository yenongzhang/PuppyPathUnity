using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class VenueSpatialAnchorBootstrap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VenueMapDefinition mapDefinition;
    [SerializeField] private Transform venueContentRoot;

    [Header("Anchor")]
    [SerializeField] private bool createAnchorOnStart;
    [SerializeField] private bool parentVenueContentToAnchor = true;
    [SerializeField] private bool replaceExistingAnchorWhenCreating = true;
    [SerializeField] private bool trySaveAnchorToDevice;
    [SerializeField] private string playerPrefsAnchorUuidKey = "PuppyPath.VenueOriginAnchorUuid";

    [Header("Debug")]
    [SerializeField] private bool logAnchorEvents = true;
    [SerializeField] private GameObject anchorMarkerPrefab;

    private Component spatialAnchorComponent;
    private Transform anchorTransform;

    private void Start()
    {
        if (createAnchorOnStart)
            CreateAnchorAtVenueOrigin();
    }

    [ContextMenu("Create Anchor At Venue Origin")]
    public void CreateAnchorAtVenueOrigin()
    {
        if (replaceExistingAnchorWhenCreating)
            ClearRuntimeAnchorObject();

        CreateAnchorAtVenueOriginInternal();
    }

    public void ReplaceAnchorAtVenueOrigin()
    {
        ClearRuntimeAnchorObject();
        CreateAnchorAtVenueOriginInternal();
    }

    private void CreateAnchorAtVenueOriginInternal()
    {
        if (venueContentRoot == null)
        {
            Debug.LogWarning("VenueSpatialAnchorBootstrap: VenueContentRoot is missing.");
            return;
        }

        Type anchorType = FindType("OVRSpatialAnchor");
        if (anchorType == null)
        {
            Debug.LogWarning("VenueSpatialAnchorBootstrap: OVRSpatialAnchor type not found. Enable/import Meta XR SDK and Anchor Support.");
            return;
        }

        GameObject anchorObject = new GameObject("VenueOriginSpatialAnchor");
        anchorTransform = anchorObject.transform;
        anchorTransform.position = GetVenueOriginWorldPosition();
        anchorTransform.rotation = venueContentRoot.rotation;

        if (anchorMarkerPrefab != null)
            Instantiate(anchorMarkerPrefab, anchorTransform.position, anchorTransform.rotation, anchorTransform);

        spatialAnchorComponent = anchorObject.AddComponent(anchorType);

        if (parentVenueContentToAnchor)
            venueContentRoot.SetParent(anchorTransform, true);

        StartCoroutine(WaitForAnchorCreation(anchorType, spatialAnchorComponent));

        if (logAnchorEvents)
            Debug.Log("VenueSpatialAnchorBootstrap: created OVRSpatialAnchor at VenueOrigin.");
    }

    [ContextMenu("Forget Saved Anchor UUID")]
    public void ForgetSavedAnchorUuid()
    {
        PlayerPrefs.DeleteKey(playerPrefsAnchorUuidKey);
        PlayerPrefs.Save();
    }

    private IEnumerator WaitForAnchorCreation(Type anchorType, Component anchorComponent)
    {
        if (anchorComponent == null)
            yield break;

        PropertyInfo createdProperty = anchorType.GetProperty("Created", BindingFlags.Instance | BindingFlags.Public);
        PropertyInfo uuidProperty = anchorType.GetProperty("Uuid", BindingFlags.Instance | BindingFlags.Public);

        float timeout = 8f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (anchorComponent == null)
                yield break;

            elapsed += Time.deltaTime;

            bool created = createdProperty == null || Convert.ToBoolean(createdProperty.GetValue(anchorComponent));
            if (created)
            {
                if (uuidProperty != null)
                {
                    object uuidValue = uuidProperty.GetValue(anchorComponent);
                    if (uuidValue != null)
                    {
                        PlayerPrefs.SetString(playerPrefsAnchorUuidKey, uuidValue.ToString());
                        PlayerPrefs.Save();
                    }
                }

                if (trySaveAnchorToDevice)
                    TryInvokeSaveAnchorAsync(anchorType, anchorComponent);

                if (logAnchorEvents)
                    Debug.Log("VenueSpatialAnchorBootstrap: spatial anchor created.");

                yield break;
            }

            yield return null;
        }

        Debug.LogWarning("VenueSpatialAnchorBootstrap: timed out waiting for spatial anchor creation.");
    }

    private void TryInvokeSaveAnchorAsync(Type anchorType, Component anchorComponent)
    {
        if (anchorComponent == null)
            return;

        MethodInfo saveMethod = anchorType.GetMethod("SaveAnchorAsync", BindingFlags.Instance | BindingFlags.Public);
        if (saveMethod == null)
        {
            Debug.LogWarning("VenueSpatialAnchorBootstrap: SaveAnchorAsync not found on this OVRSpatialAnchor version.");
            return;
        }

        try
        {
            saveMethod.Invoke(anchorComponent, null);
            if (logAnchorEvents)
                Debug.Log("VenueSpatialAnchorBootstrap: invoked SaveAnchorAsync. Check device logs for final save result if needed.");
        }
        catch (Exception exception)
        {
            Debug.LogWarning("VenueSpatialAnchorBootstrap: SaveAnchorAsync invocation failed: " + exception.Message);
        }
    }

    private Vector3 GetVenueOriginWorldPosition()
    {
        Vector3 localOrigin = mapDefinition != null ? mapDefinition.originWorldPosition : Vector3.zero;
        return venueContentRoot.TransformPoint(localOrigin);
    }

    private void ClearRuntimeAnchorObject()
    {
        if (anchorTransform == null)
            return;

        Transform previousAnchor = anchorTransform;

        if (venueContentRoot != null && venueContentRoot.parent == previousAnchor)
            venueContentRoot.SetParent(null, true);

        spatialAnchorComponent = null;
        anchorTransform = null;

        if (Application.isPlaying)
            Destroy(previousAnchor.gameObject);
        else
            DestroyImmediate(previousAnchor.gameObject);
    }

    private static Type FindType(string typeName)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (Assembly assembly in assemblies)
        {
            Type type = assembly.GetType(typeName);
            if (type != null)
                return type;
        }

        foreach (Assembly assembly in assemblies)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                types = exception.Types;
            }

            foreach (Type type in types)
            {
                if (type != null && type.Name == typeName)
                    return type;
            }
        }

        return null;
    }
}
