using UnityEngine;
using UnityEngine.EventSystems;

public class VenueMapOpenButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private NavigationController navigationController;
    [SerializeField] private VenueMapUiController mapUiController;

    public void OnPointerClick(PointerEventData eventData)
    {
        OpenLargeMap();
    }

    public void OpenLargeMap()
    {
        if (navigationController != null)
            navigationController.OpenMapFromHud();
        else if (mapUiController != null)
            mapUiController.OpenLargeMap();
    }

    private void Awake()
    {
        if (navigationController == null)
            navigationController = FindFirstObjectByType<NavigationController>();

        if (mapUiController == null)
            mapUiController = FindFirstObjectByType<VenueMapUiController>();
    }
}
