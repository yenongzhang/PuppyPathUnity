using UnityEngine;
using UnityEngine.EventSystems;

public class VenueMapOpenButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private PuppyPathV2FlowController flowController;
    [SerializeField] private VenueMapUiController mapUiController;

    public void OnPointerClick(PointerEventData eventData)
    {
        OpenLargeMap();
    }

    public void OpenLargeMap()
    {
        if (flowController != null)
            flowController.OpenBigMap();
        else if (mapUiController != null)
            mapUiController.OpenLargeMap();
    }
}
