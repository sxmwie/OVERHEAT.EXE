using System;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class AdPopup : MonoBehaviour
{
    public enum AdType
    {
        Normal,
        Bomb,
        Cascade
    }

    [Header("Setup")]
    public Button closeButton;
    public AdType adType = AdType.Normal;

    [Header("Auto Despawn (Bomb / Cascade)")]
    public bool autoDespawn = false;
    public float autoDespawnTime = 6f;

    // ───────── MOVEMENT (DRIFT) ─────────
    [Header("Drift Movement")]
    public bool isMovingAd = false;
    public float moveSpeed = 45f;
    public float driftDistance = 25f;
    public float directionChangeInterval = 1.2f;

    // ───────── BREATHING (SCALE WOBBLE) ─────────
    [Header("Breathing")]
    public bool breathing = false;
    public float breathingSpeed = 2f;
    public float breathingStrength = 0.05f;

    // ───────── ROTATION ─────────
    [Header("Rotation")]
    public bool rotating = false;
    public float rotationSpeed = 25f;

    // ───────── RANDOM JUMPS ─────────
    [Header("Random Jumps")]
    public bool jumping = false;                 // enable for “jumpy” ads
    public float jumpIntervalMin = 0.8f;
    public float jumpIntervalMax = 1.8f;
    public float jumpDistance = 25f;

    // ───────── OPEN / CLOSE ANIM ─────────
    [Header("Open / Close Animation")]
    public float spawnScale = 0.2f;
    public float openDuration = 0.18f;
    public float closeDuration = 0.16f;

    // ───────── BOMB FLASH ─────────
    [Header("Bomb Flashing")]
    public bool bombFlash = true;
    public Image bombFlashTarget;
    public Color bombFlashColor = Color.red;
    public float bombFlashSpeed = 6f;

    RectTransform rt;
    float lifeTimer = 0f;

    Vector2 driftTarget;
    float driftTimer = 0f;

    // jump state
    float jumpTimer = 0f;
    float nextJumpTime = 1f;

    Vector3 baseScale;

    bool isOpening = false;
    float openTimer = 0f;

    bool isClosing = false;
    float closeTimer = 0f;
    Action onCloseFinished;

    Color bombBaseColor;
    bool bombColorCached = false;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        // baseScale will be set in Start after the safenet has a chance to resize us
    }


    void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);

        // make sure the popup is not absurdly big
        if (GameManager.Instance != null && rt != null)
        {
            GameManager.Instance.EnsurePopupFits(rt);
            GameManager.Instance.SnapPopupInside(rt);
        }

        // now that we've possibly scaled it, cache this as the "real" base scale
        baseScale = rt.localScale;
        if (baseScale == Vector3.zero)
            baseScale = Vector3.one;

        // spawn tiny → grow
        rt.localScale = baseScale * spawnScale;
        isOpening = true;
        openTimer = 0f;

        if (isMovingAd)
            PickNewDriftPoint();

        // randomise first jump delay
        nextJumpTime = Random.Range(jumpIntervalMin, jumpIntervalMax);

        if (bombFlashTarget != null)
        {
            bombBaseColor = bombFlashTarget.color;
            bombColorCached = true;
        }
    }


    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        return;
        
        float dt = Time.deltaTime;

        // ───── auto despawn (bomb/cascade ignored long enough) ─────
        if (autoDespawn && !isClosing)
        {
            lifeTimer += dt;
            if (lifeTimer >= autoDespawnTime)
            {
                StartClosing(() =>
                {
                    if (GameManager.Instance != null)
                        GameManager.Instance.OnAdAutoDespawn(this);
                });
                return;
            }
        }

        // ───── drift movement ─────
        if (isMovingAd && !isClosing)
        {
            driftTimer += dt;
            if (driftTimer >= directionChangeInterval)
            {
                driftTimer = 0f;
                PickNewDriftPoint();
            }

            rt.anchoredPosition = Vector2.MoveTowards(
                rt.anchoredPosition,
                driftTarget,
                moveSpeed * dt
            );

            if (GameManager.Instance != null)
                GameManager.Instance.SnapPopupInside(rt);
        }

        // ───── random jumps ─────
        if (jumping && !isClosing && GameManager.Instance != null)
        {
            jumpTimer += dt;
            if (jumpTimer >= nextJumpTime)
            {
                jumpTimer = 0f;
                nextJumpTime = Random.Range(jumpIntervalMin, jumpIntervalMax);

                Vector2 offset = Random.insideUnitCircle.normalized * jumpDistance;
                rt.anchoredPosition += offset;
                GameManager.Instance.SnapPopupInside(rt);
            }
        }

        // ───── scale animations (open / close / breathing) ─────
        float scaleFactor = 1f;

        // opening grow
        if (isOpening)
        {
            openTimer += dt;
            float t = Mathf.Clamp01(openTimer / openDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            float openScale = Mathf.Lerp(spawnScale, 1f, eased);
            scaleFactor *= openScale;

            if (t >= 1f)
                isOpening = false;
        }

        // closing shrink
        if (isClosing)
        {
            closeTimer += dt;
            float t = Mathf.Clamp01(closeTimer / closeDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            float closeScale = Mathf.Lerp(1f, 0f, eased);
            scaleFactor *= closeScale;

            if (t >= 1f)
            {
                if (onCloseFinished != null)
                    onCloseFinished();
                Destroy(gameObject);
                return;
            }
        }

        // breathing wobble
        if (breathing && !isClosing)
        {
            float breathe = 1f + Mathf.Sin(Time.time * breathingSpeed) * breathingStrength;
            scaleFactor *= breathe;
        }

        rt.localScale = baseScale * scaleFactor;

        // rotation
        if (rotating && !isClosing)
        {
            rt.Rotate(0f, 0f, rotationSpeed * dt);
        }

        // ───── bomb flashing ─────
        if (bombFlash && adType == AdType.Bomb && bombFlashTarget != null && !isClosing)
        {
            if (!bombColorCached)
            {
                bombBaseColor = bombFlashTarget.color;
                bombColorCached = true;
            }

            float t = (Mathf.Sin(Time.time * bombFlashSpeed) + 1f) * 0.5f; // 0..1
            bombFlashTarget.color = Color.Lerp(bombBaseColor, bombFlashColor, t);
        }
        else if (bombFlashTarget != null && bombColorCached)
        {
            bombFlashTarget.color = bombBaseColor;
        }
    }

    void PickNewDriftPoint()
    {
        Vector2 randomOffset = Random.insideUnitCircle * driftDistance;
        driftTarget = rt.anchoredPosition + randomOffset;
    }

    void OnCloseClicked()
    {
        if (GameManager.Instance == null || isClosing)
            return;

        // play click / close SFX
        GameManager.Instance.PlayAdCloseSfx();

        Action cb = null;

        switch (adType)
        {
            case AdType.Normal:
                cb = () => GameManager.Instance.OnPopupClosed(this);
                // show "+2 cooling" at this popup
                GameManager.Instance.ShowAdCoolingText(rt);
                break;

            case AdType.Bomb:
                cb = () => GameManager.Instance.OnBombAdClicked(this);
                break;

            case AdType.Cascade:
                cb = () => GameManager.Instance.OnCascadeAdClosed(this);
                break;
        }

        StartClosing(cb);
    }

    void StartClosing(Action callback)
    {
        if (isClosing) return;

        // spawn particles at this popup
        if (GameManager.Instance != null && rt != null)
            GameManager.Instance.SpawnAdCloseFx(rt);

        isClosing = true;
        isOpening = false;
        closeTimer = 0f;
        onCloseFinished = callback;

        if (closeButton != null)
            closeButton.interactable = false;
    }
}
