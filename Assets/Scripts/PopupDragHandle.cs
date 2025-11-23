using UnityEngine;
using UnityEngine.EventSystems;

public class PopupDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [Tooltip("The root window RectTransform (the whole popup). If left empty, will try to find AdPopup in parents.")]
    public RectTransform window;

    RectTransform parentRect;  // usually PopupArea
    Vector2 dragOffset;

    void Awake()
    {
        if (window == null)
        {
            AdPopup ap = GetComponentInParent<AdPopup>();
            if (ap != null)
                window = ap.GetComponent<RectTransform>();
        }

        if (window != null)
            parentRect = window.parent as RectTransform;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (window == null || parentRect == null) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        // store offset so it doesn't snap to the mouse center when you start dragging
        dragOffset = window.anchoredPosition - localPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (window == null || parentRect == null) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        // move window with mouse + offset
        window.anchoredPosition = localPoint + dragOffset;

        // clamp inside popup area using GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SnapPopupInside(window);
        }
    }
}
