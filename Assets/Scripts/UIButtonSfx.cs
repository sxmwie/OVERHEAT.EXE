using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonSfx : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
{
    [Header("What to play")]
    public bool playClick = true;
    public bool playHover = true;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (playClick && UISfxPlayer.Instance != null)
            UISfxPlayer.Instance.PlayClick();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (playHover && UISfxPlayer.Instance != null)
            UISfxPlayer.Instance.PlayHover();
    }
}
