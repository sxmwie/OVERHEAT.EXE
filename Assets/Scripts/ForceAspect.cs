using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ForceAspect : MonoBehaviour
{
    [Tooltip("Your desired game aspect ratio")]
    public float targetAspect = 4f / 3f;   // 4:3

    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        float windowAspect = (float)Screen.width / Screen.height;
        float scale = windowAspect / targetAspect;

        if (scale < 1f)
        {
            // window is "taller" → black bars left/right
            float width = scale;
            float x = (1f - width) * 0.5f;
            cam.rect = new Rect(x, 0f, width, 1f);
        }
        else
        {
            // window is "wider" → black bars top/bottom
            float height = 1f / scale;
            float y = (1f - height) * 0.5f;
            cam.rect = new Rect(0f, y, 1f, height);
        }
    }
}
