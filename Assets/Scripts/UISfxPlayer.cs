using UnityEngine;

public class UISfxPlayer : MonoBehaviour
{
    public static UISfxPlayer Instance;

    [Header("Audio Source")]
    [Tooltip("AudioSource used to play UI sounds (no clip, Play On Awake OFF, Loop OFF).")]
    public AudioSource source;

    [Header("Clips")]
    public AudioClip clickClip;   // button + general UI click
    public AudioClip hoverClip;   // when hovering over a button

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (source == null)
            source = GetComponent<AudioSource>();
    }

    public void PlayClick()
    {
        if (source != null && clickClip != null)
            source.PlayOneShot(clickClip);
    }

    public void PlayHover()
    {
        if (source != null && hoverClip != null)
            source.PlayOneShot(hoverClip);
    }
}
