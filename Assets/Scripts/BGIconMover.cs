using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class BGIconMover : MonoBehaviour
{
    RectTransform rt;
    Image img;

    Vector2 velocity;
    float angularSpeed;
    float lifeTime;
    float maxLifeTime;

    float baseScale;
    float wobbleAmount;
    float wobbleSpeed;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        img = GetComponent<Image>();
    }

    // called right after spawn to configure this icon
    public void Init(Vector2 vel, float angSpeed, float life, float startScale, float wobbleAmp, float wobbleSpd)
    {
        velocity = vel;
        angularSpeed = angSpeed;
        maxLifeTime = life;
        lifeTime = 0f;

        baseScale = startScale;
        wobbleAmount = wobbleAmp;
        wobbleSpeed = wobbleSpd;

        // start small fade-in
        Color c = img.color;
        c.a = 0f;
        img.color = c;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        lifeTime += dt;

        // move
        rt.anchoredPosition += velocity * dt;

        // rotate
        rt.localRotation *= Quaternion.Euler(0f, 0f, angularSpeed * dt);

        // breathing scale
        float wobble = Mathf.Sin(lifeTime * wobbleSpeed) * wobbleAmount;
        float scale = baseScale * (1f + wobble);
        rt.localScale = Vector3.one * scale;

        // fade in first 20% of life, fade out last 30%
        float a = 1f;
        float t01 = lifeTime / maxLifeTime;
        if (t01 < 0.2f)
        {
            a = Mathf.InverseLerp(0f, 0.2f, t01);          // 0 → 1
        }
        else if (t01 > 0.7f)
        {
            a = Mathf.InverseLerp(1f, 0.7f, t01);          // 1 → 0
        }

        Color c = img.color;
        c.a = a;
        img.color = c;

        if (lifeTime >= maxLifeTime)
        {
            Destroy(gameObject);
        }
    }
}
