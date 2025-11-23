using UnityEngine;
using TMPro;

public class FloatingTextUI : MonoBehaviour
{
    public TMP_Text tmpText;

    [Header("Motion")]
    public float moveDistance = 40f;   // how far it moves down
    public float duration = 0.9f;      // how long until it disappears

    Vector2 startPos;
    Vector2 endPos;
    float timer = 0f;
    RectTransform rt;

    public void Setup(string message, Color color)
    {
        if (tmpText == null)
            tmpText = GetComponentInChildren<TMP_Text>();

        if (tmpText != null)
        {
            tmpText.text = message;
            tmpText.color = color;
        }

        if (rt == null)
            rt = GetComponent<RectTransform>();

        startPos = rt.anchoredPosition;
        endPos = startPos + Vector2.down * moveDistance;   // move down
    }

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        if (tmpText == null)
            tmpText = GetComponentInChildren<TMP_Text>();
    }

    void Start()
    {
        // in case Setup was not called yet, initialise positions
        startPos = rt.anchoredPosition;
        endPos = startPos + Vector2.down * moveDistance;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);
        float eased = Mathf.SmoothStep(0f, 1f, t);

        // move
        rt.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);

        // fade
        if (tmpText != null)
        {
            Color c = tmpText.color;
            c.a = 1f - t;
            tmpText.color = c;
        }

        if (t >= 1f)
            Destroy(gameObject);
    }
}
