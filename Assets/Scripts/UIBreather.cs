using UnityEngine;

public class UIBreather : MonoBehaviour
{
    [Header("Breathing Settings")]
    public float scaleAmplitude = 0.05f;  // how big the pulse is (0.05 = 5%)
    public float scaleSpeed = 1.2f;       // how fast it breathes
    public bool useUnscaledTime = true;   // keep breathing even if timescale = 0

    Vector3 baseScale;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        float t = useUnscaledTime ? Time.unscaledTime : Time.time;
        float s = 1f + Mathf.Sin(t * scaleSpeed) * scaleAmplitude;

        transform.localScale = baseScale * s;
    }
}
