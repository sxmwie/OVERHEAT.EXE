using UnityEngine;

public class HoverUI : MonoBehaviour
{
    public float amplitude = 8f;
    public float speed = 2f;

    Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        float y = Mathf.Sin(Time.unscaledTime * speed) * amplitude;
        transform.localPosition = startPos + new Vector3(0, y, 0);
    }
}
