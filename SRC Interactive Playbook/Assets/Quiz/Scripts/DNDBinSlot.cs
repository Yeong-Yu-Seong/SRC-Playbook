using UnityEngine;
using UnityEngine.EventSystems;

public class DNDBinSlot : MonoBehaviour, IDropHandler
{
    [Tooltip("The ID of this specific bin (e.g., choice_archive)")]
    [HideInInspector] public string binChoiceId;

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObject = eventData.pointerDrag;
        if (droppedObject != null)
        {
            DraggableOption draggable = droppedObject.GetComponent<DraggableOption>();
            if (draggable != null)
            {
                draggable.parentAfterDrag = this.transform;
            }
        }
    }
}