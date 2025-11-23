using System;
using UnityEngine;
using UnityEngine.UI;

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

    // ───── Extra Behaviours (you already had) ─────
    [Header("Movement")]
    public bool isMovingAd = false;
    public float moveSpeed = 45f;
    public float driftDistance = 25f;
    public float directionChangeInterval = 1.2f;

    [Header("Breathing")]
    public bool breathing = false;
    public float breathingSpeed = 2f;
    public float breathingStrength = 0.05f;

    [Header("Rotation")]
    public bool rotating = false;
    public float rotationSpeed = 25f;

    [Header("Run Away From Cursor")]
    public bool runFromCursor = false;
    public float runSpeed = 200f;
    public float cursorRunDistance = 110f;

    // ───── NEW: open / close animations ─────
    [Header("Open / Close Animation")]
    public float spawnScale = 0.2f;      // start at 20% size, grow to 100%
    public float openDuration = 0.18f;   // seconds
    public float closeDuration = 0.16f;  // seconds

    RectTransform rt;
    float lifeTimer = 0f;

    Vector2 driftTarget;
    float driftTimer = 0f;

    Vector3 baseScale;

    bool isOpening = false;
    float openTimer = 0f;

    bool isClosing = false;
    float closeTimer = 0f;
    Action onCloseFinished;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        baseScale = rt.localScale;
        if (baseScale == Vector3.zero)
            baseScale = Vector3.one;
    }

    void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);

        if (GameManager.Instance != null && rt != null)
            GameManager.Instance.SnapPopupInside(rt);

        // start tiny, then grow
        rt.localScale = baseScale * spawnScale;
        isOpening = true;
        openTimer = 0f;

        if (isMovingAd)
            PickNewDriftPoint();
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // ───── Auto despawn ─────
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

        // ───── Movement / drift ─────
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

        // ───── Run away from cursor ─────
        if (runFromCursor && !isClosing && GameManager.Instance != null)
        {
            Vector2 mouseLocal;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                GameManager.Instance.popupArea,
                Input.mousePosition,
                null,
                out mouseLocal
            );

            float dist = Vector2.Distance(mouseLocal, rt.anchoredPosition);
            if (dist < cursorRunDistance)
            {
                Vector2 away = (rt.anchoredPosition - mouseLocal).normalized;
                rt.anchoredPosition += away * runSpeed * dt;
                GameManager.Instance.SnapPopupInside(rt);
            }
        }

        // ───── Breathing + rotation + open/close scale ─────
        float scaleFactor = 1f;

        // opening (spawn grow)
        if (isOpening)
        {
            openTimer += dt;
            float t = Mathf.Clamp01(openTimer / openDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            float openScale = Mathf.Lerp(spawnScale, 1f, eased);
            scaleFactor *= openScale;

            if (t >= 1f)
            {
                isOpening = false;
            }
        }

        // closing (shrink away)
        if (isClosing)
        {
            closeTimer += dt;
            float t = Mathf.Clamp01(closeTimer / closeDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            float closeScale = Mathf.Lerp(1f, 0f, eased);
            scaleFactor *= closeScale;

            if (t >= 1f)
            {
                // end of close animation
                if (onCloseFinished != null)
                    onCloseFinished();
                Destroy(gameObject);
                return;
            }
        }

        // breathing multiplier
        if (breathing && !isClosing)
        {
            float breathe = 1f + Mathf.Sin(Time.time * breathingSpeed) * breathingStrength;
            scaleFactor *= breathe;
        }

        // apply final scale
        rt.localScale = baseScale * scaleFactor;

        // rotation
        if (rotating && !isClosing)
        {
            rt.Rotate(0f, 0f, rotationSpeed * dt);
        }
    }

    void PickNewDriftPoint()
    {
        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * driftDistance;
        driftTarget = rt.anchoredPosition + randomOffset;
    }

    void OnCloseClicked()
    {
        if (GameManager.Instance == null || isClosing)
            return;

        Action cb = null;

        switch (adType)
        {
            case AdType.Normal:
                cb = () =>
                {
                    GameManager.Instance.OnPopupClosed(this);
                };
                break;

            case AdType.Bomb:
                cb = () =>
                {
                    GameManager.Instance.OnBombAdClicked(this);
                };
                break;

            case AdType.Cascade:
                cb = () =>
                {
                    GameManager.Instance.OnCascadeAdClosed(this);
                };
                break;
        }

        StartClosing(cb);
    }

    void StartClosing(Action callback)
    {
        if (isClosing) return;

        isClosing = true;
        isOpening = false;
        closeTimer = 0f;
        onCloseFinished = callback;

        if (closeButton != null)
            closeButton.interactable = false;
    }
}
