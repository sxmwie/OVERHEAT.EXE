using UnityEngine;
using UnityEngine.EventSystems;

/// Put this on any UI object (button, text, etc.) that should breathe.
/// Also reacts to mouse hover (gets bigger + stronger breathing).
[RequireComponent(typeof(RectTransform))]
public class UIButtonBreather : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Breathing")]
    public float breathAmplitude = 0.03f;   // size of the breathing
    public float breathSpeed = 1.5f;        // how fast it breathes

    [Header("Hover")]
    public float hoverScaleMultiplier = 1.1f; // global scale boost on hover
    public float hoverExtraAmplitude = 0.02f; // extra breathing when hovered

    Vector3 baseScale;
    float timer;
    bool isHover = false;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    void OnEnable()
    {
        timer = 0f;
    }

    void Update()
    {
        // use unscaled time so breathing keeps going even if timeScale changes
        timer += Time.unscaledDeltaTime * breathSpeed;

        float amp = breathAmplitude + (isHover ? hoverExtraAmplitude : 0f);
        float breathe = 1f + Mathf.Sin(timer) * amp;

        float hoverMul = isHover ? hoverScaleMultiplier : 1f;

        transform.localScale = baseScale * breathe * hoverMul;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHover = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHover = false;
    }
}
