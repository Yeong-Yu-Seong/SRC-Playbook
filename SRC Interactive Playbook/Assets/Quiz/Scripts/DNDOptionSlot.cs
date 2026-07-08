using UnityEngine;
using UnityEngine.EventSystems;

public class DNDOptionSlot : MonoBehaviour, IDropHandler
{
    [HideInInspector] public string questionId;
    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObject = eventData.pointerDrag;
        if (droppedObject != null)
        {
            DraggableOption draggable = droppedObject.GetComponent<DraggableOption>();
            if (draggable != null)
            {
                // Checks if a draggable option is already sitting in this slot
                DraggableOption existingAnswer = GetComponentInChildren<DraggableOption>();

                if (existingAnswer == null)
                {
                    draggable.parentAfterDrag = this.transform;
                }
                else
                {
                    // Swaps the answers if the user drops a new one into a full slot
                    existingAnswer.transform.SetParent(draggable.parentAfterDrag);
                    draggable.parentAfterDrag = this.transform;
                }
            }
        }
    }
}