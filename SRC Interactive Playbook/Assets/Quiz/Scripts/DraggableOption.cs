using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableOption : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public string choiceId;

    private CanvasGroup canvasGroup;
    [HideInInspector] public Transform parentAfterDrag;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        parentAfterDrag = transform.parent;
        transform.SetParent(transform.root);
        transform.SetAsLastSibling();

        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        transform.SetParent(parentAfterDrag);

        // Snap perfectly to the center of the new parent slot
        transform.localPosition = Vector3.zero;

        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
    }
}