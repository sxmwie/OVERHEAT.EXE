using UnityEngine;
using UnityEngine.UI;

public class TemperatureBar : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The rect that visually scales left→right for the fill")]
    public RectTransform fillRect;

    [Tooltip("Optional: the Image whose color we tint")]
    public Image fillImage;

    [Header("Colors")]
    public Color coldColor   = new Color(0.2f, 0.6f, 1f);   // blue
    public Color hotColor    = new Color(1f, 0.2f, 0.1f);   // red
    public Color frozenColor = new Color(0.6f, 0.8f, 1.2f); // light icy blue

    [Header("Cool Flash")]
    [Tooltip("How long the 'cool' flash lasts (seconds)")]
    public float coolFlashDuration = 0.25f;

    float currentValue01 = 0f;       // 0–1
    bool isFrozen = false;
    float coolFlashTimer = 0f;

    void Awake()
    {
        // Safe defaults
        if (fillRect == null)
            fillRect = GetComponent<RectTransform>();

        if (fillImage == null)
            fillImage = GetComponent<Image>();
    }

    void Update()
    {
        // tick down the cool flash timer
        if (coolFlashTimer > 0f)
        {
            coolFlashTimer -= Time.unscaledDeltaTime;   // unscaled so it still finishes even near game over
            if (coolFlashTimer < 0f) coolFlashTimer = 0f;
        }

        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        // scale the fill horizontally by value
        if (fillRect != null)
        {
            Vector3 s = fillRect.localScale;

            // keep width the same, only scale height
            s.x = 1f;
            s.y = Mathf.Max(0.0001f, currentValue01);   // 0–1 height

            fillRect.localScale = s;
        }


        if (fillImage == null) return;

        Color finalColor;

        if (isFrozen)
        {
            // fully frozen = always icy blue
            finalColor = frozenColor;
        }
        else if (coolFlashTimer > 0f)
        {
            // short cool flash: lerp from frozenColor back to normal gradient over time
            float k = 1f - Mathf.Clamp01(coolFlashTimer / coolFlashDuration);
            Color normal = Color.Lerp(coldColor, hotColor, currentValue01);
            finalColor = Color.Lerp(frozenColor, normal, k);
        }
        else
        {
            // normal behavior: gradient cold → hot
            finalColor = Color.Lerp(coldColor, hotColor, currentValue01);
        }

        fillImage.color = finalColor;
    }

    // ---------- API called from GameManager ----------

    /// <summary>Set normalized value 0–1 (0 = min temp, 1 = max temp)</summary>
    public void SetValue01(float value01)
    {
        currentValue01 = Mathf.Clamp01(value01);
        UpdateVisuals();
    }

    /// <summary>Enable or disable frozen mode (bar stays icy blue)</summary>
    public void SetFrozen(bool frozen)
    {
        isFrozen = frozen;
        if (frozen)
        {
            // when entering frozen state, cancel any cool flash
            coolFlashTimer = 0f;
        }
        UpdateVisuals();
    }

    /// <summary>Triggers a short blue flash (used on cool powerup).</summary>
    public void TriggerCoolFlash()
    {
        // if already frozen, don't override that visual
        if (isFrozen) return;

        coolFlashTimer = coolFlashDuration;
        UpdateVisuals();
    }
}
