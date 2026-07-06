using UnityEngine;
using UnityEngine.EventSystems;

public class DNDOptionSlot : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {   
        if (transform.childCount == 0)
        {
            GameObject droppedObject = eventData.pointerDrag; // gets the object that was dropped onto this slot
            DraggableOption draggableOption = droppedObject.GetComponent<DraggableOption>();
            draggableOption.parentAfterDrag = this.transform; // sets the parent of the dropped object to this slot
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
