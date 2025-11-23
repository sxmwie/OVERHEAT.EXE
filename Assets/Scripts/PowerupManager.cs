using UnityEngine;
using Random = UnityEngine.Random;

public class PowerupManager : MonoBehaviour
{
    public static PowerupManager Instance;

    [Header("UI")]
    public RectTransform powerupBar;   // the bar across the bottom
    public RectTransform hitZone;     // the central box
    public Transform moverParent;     // parent for moving icon (PowerupMover)

    [Header("Prefabs")]
    public GameObject powerFreeze;
    public GameObject powerCool;
    public GameObject powerClear;

    [Header("Spawn Chances")]
    [Range(0f, 1f)] public float chanceFreeze = 0.5f;
    [Range(0f, 1f)] public float chanceCool   = 0.35f;
    [Range(0f, 1f)] public float chanceClear  = 0.15f;

    [Header("Movement Speed")]
    public float minSpeed = 260f;
    public float maxSpeed = 620f;   // bigger range for more randomness

    [Header("Spawn Timing")]
    public float minDelay = 4f;
    public float maxDelay = 11f;    // more random between spawns

    [Header("Visuals")]
    public bool rotateIcon = true;
    public float rotateSpeed = 220f;

    [Tooltip("Fraction of the bar width at each edge used for fading in/out.")]
    [Range(0.05f, 0.45f)]
    public float edgeFadeFraction = 0.25f;

    [Header("Collect FX")]
    public GameObject collectFxPrefab;  // particle burst prefab
    public Transform collectFxParent;   // usually same as moverParent or the canvas

    [Header("Collect Audio")]
    public AudioSource sfxSource;
    public AudioClip coolClip;
    public AudioClip freezeClip;
    public AudioClip clearClip;

    GameObject currentIcon;
    RectTransform currentRT;
    CanvasGroup currentCG;

    float speed;
    bool active = false;
    bool animating = false;  // true during collect/miss animation

    float spawnTimer = 0f;
    float nextSpawnDelay = 7f; // will be randomized in Start()

    float startX;
    float endX;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // randomize the very first spawn too
        nextSpawnDelay = Random.Range(minDelay, maxDelay);
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        return;

        if (!active)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= nextSpawnDelay)
                SpawnOne();
            return;
        }

        if (!animating)
        {
            // move
            currentRT.anchoredPosition += Vector2.left * speed * Time.deltaTime;

            float x = currentRT.anchoredPosition.x;

            // rotate icon while it travels
            if (rotateIcon)
            {
                currentRT.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
            }

            // fade based on x position
            UpdateFadeForPosition(x);

            // missed automatically & went off the left side -> fade out and clean up
            if (x <= endX)
            {
                // simple auto-miss without spacebar -> just end
                EndIcon(false);
                return;
            }

            // catch attempt
            if (Input.GetKeyDown(KeyCode.Space))
                CheckCatch();
        }
    }

    void SpawnOne()
    {
        spawnTimer = 0f;
        nextSpawnDelay = Random.Range(minDelay, maxDelay);

        currentIcon = Instantiate(SelectRandomPrefab(), moverParent);
        currentRT = currentIcon.GetComponent<RectTransform>();
        currentRT.localScale = Vector3.one;

        // compute edges based on bar width
        float halfBarWidth = powerupBar.rect.width / 2f;

        // start just off the right side, end just off the left
        startX = halfBarWidth + 60f;
        endX   = -halfBarWidth - 60f;

        currentRT.anchoredPosition = new Vector2(startX, 0f);

        speed = Random.Range(minSpeed, maxSpeed);

        // set up canvas group for fade
        currentCG = currentIcon.GetComponent<CanvasGroup>();
        if (currentCG == null)
            currentCG = currentIcon.AddComponent<CanvasGroup>();

        currentCG.alpha = 0f;  // start invisible

        active = true;
        animating = false;
    }

    GameObject SelectRandomPrefab()
    {
        float r = Random.value;
        if (r < chanceClear) return powerClear;
        if (r < chanceClear + chanceCool) return powerCool;
        return powerFreeze;
    }

    void CheckCatch()
    {
        float x = currentRT.anchoredPosition.x;
        float zoneHalf = hitZone.rect.width / 2f;

        if (Mathf.Abs(x) <= zoneHalf)
        {
            // caught → add to inventory, then play collect animation with fx + sound
            ApplyPowerup(currentIcon.name);
            StartCoroutine(CollectAnimation());
        }
        else
        {
            // missed with spacebar → pause and pulse out
            StartCoroutine(MissPulseAnimation());
        }

        animating = true;
    }

    void EndIcon(bool triggered)
    {
        active = false;
        animating = false;

        if (currentIcon != null)
            Destroy(currentIcon);

        currentIcon = null;
        currentRT = null;
        currentCG = null;
    }

    void ApplyPowerup(string name)
    {
        if (GameManager.Instance == null) return;

        if (name.Contains("Freeze"))
        {
            GameManager.Instance.AddPowerupFreeze(1);
        }
        else if (name.Contains("Cool"))
        {
            GameManager.Instance.AddPowerupCool(1);
        }
        else if (name.Contains("Clear"))
        {
            GameManager.Instance.AddPowerupClear(1);
        }

        PlayCollectSound(name);
        SpawnCollectFx();
    }

    void PlayCollectSound(string name)
    {
        if (sfxSource == null) return;

        AudioClip clip = null;
        if (name.Contains("Freeze"))      clip = freezeClip;
        else if (name.Contains("Cool"))   clip = coolClip;
        else if (name.Contains("Clear"))  clip = clearClip;

        if (clip != null)
            sfxSource.PlayOneShot(clip);
    }

    void SpawnCollectFx()
    {
        if (collectFxPrefab == null || hitZone == null) return;

        Transform parent = collectFxParent != null ? collectFxParent : hitZone.parent;

        // spawn at hitzone position
        GameObject fx = Instantiate(collectFxPrefab, parent);
        fx.transform.position = hitZone.transform.position;
    }



    // ─────────────────────────────────────────────
    // FADE LOGIC – based purely on X position
    // fades in near right edge, full opacity around centre,
    // fades out as it exits left if you never press space.
    // ─────────────────────────────────────────────
    void UpdateFadeForPosition(float x)
    {
        if (currentCG == null || powerupBar == null) return;

        float barWidth = powerupBar.rect.width;
        float halfBar  = barWidth / 2f;

        // plateau region where icon is fully visible
        float plateauRight = halfBar - barWidth * edgeFadeFraction;
        float plateauLeft  = -plateauRight;

        float alpha = 1f;

        if (x > plateauRight)
        {
            // fade IN as it travels from offscreen (startX) to plateauRight
            alpha = Mathf.InverseLerp(startX, plateauRight, x);
        }
        else if (x < plateauLeft)
        {
            // fade OUT as it travels from plateauLeft to completely off (endX)
            alpha = Mathf.InverseLerp(endX, plateauLeft, x);
        }
        else
        {
            // centre zone → fully visible
            alpha = 1f;
        }

        currentCG.alpha = Mathf.Clamp01(alpha);
    }

    // ─────────────────────────────────────────────
    // COLLECT ANIMATION – little pop & fade
    // ─────────────────────────────────────────────
    System.Collections.IEnumerator CollectAnimation()
    {
        if (currentRT == null || currentCG == null)
        {
            EndIcon(true);
            yield break;
        }

        Vector3 startScale = currentRT.localScale;
        Vector3 endScale   = startScale * 1.3f;

        float startAlpha = currentCG.alpha;
        float duration   = 0.25f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;

            currentRT.localScale = Vector3.Lerp(startScale, endScale, k);
            currentCG.alpha      = Mathf.Lerp(startAlpha, 0f, k);

            yield return null;
        }

        EndIcon(true);
    }

    // ─────────────────────────────────────────────
    // MISS PULSE ANIMATION – freeze and pulse out
    // ─────────────────────────────────────────────
    System.Collections.IEnumerator MissPulseAnimation()
    {
        if (currentRT == null || currentCG == null)
        {
            EndIcon(false);
            yield break;
        }

        // stop rotation/movement; we just sit there and pulse
        Vector3 startScale = currentRT.localScale;
        Vector3 endScale   = startScale * 1.4f;

        float startAlpha = currentCG.alpha <= 0f ? 1f : currentCG.alpha;
        float duration   = 0.4f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;

            currentRT.localScale = Vector3.Lerp(startScale, endScale, k);
            currentCG.alpha      = Mathf.Lerp(startAlpha, 0f, k);

            yield return null;
        }

        EndIcon(false);
    }
}
