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

    // ───── Additional Behaviours ─────
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

    // ───── internals ─────
    RectTransform rt;
    float lifeTimer = 0f;
    Vector2 driftTarget;
    float driftTimer = 0f;
    Vector3 baseScale;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        baseScale = rt.localScale;
    }

    void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);

        if (GameManager.Instance != null && rt != null)
            GameManager.Instance.SnapPopupInside(rt);

        if (isMovingAd)
            PickNewDriftPoint();
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // auto despawn
        if (autoDespawn)
        {
            lifeTimer += dt;
            if (lifeTimer >= autoDespawnTime)
            {
                AutoDespawn();
                return;
            }
        }

        // drifting movement
        if (isMovingAd)
        {
            driftTimer += dt;
            if (driftTimer >= directionChangeInterval)
            {
                driftTimer = 0f;
                PickNewDriftPoint();
            }

            rt.anchoredPosition = Vector2.MoveTowards(rt.anchoredPosition, driftTarget, moveSpeed * dt);

            if (GameManager.Instance != null)
                GameManager.Instance.SnapPopupInside(rt);
        }

        // breathing
        if (breathing)
        {
            float scale = 1f + Mathf.Sin(Time.time * breathingSpeed) * breathingStrength;
            rt.localScale = baseScale * scale;
        }

        // rotation
        if (rotating)
        {
            rt.Rotate(0f, 0f, rotationSpeed * dt);
        }

        // run from cursor
        if (runFromCursor)
        {
            Vector2 mousePos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                GameManager.Instance.popupArea,
                Input.mousePosition,
                null,
                out mousePos
            );

            float dist = Vector2.Distance(mousePos, rt.anchoredPosition);
            if (dist < cursorRunDistance)
            {
                Vector2 away = (rt.anchoredPosition - mousePos).normalized;
                rt.anchoredPosition += away * runSpeed * dt;
                if (GameManager.Instance != null)
                    GameManager.Instance.SnapPopupInside(rt);
            }
        }
    }

    void PickNewDriftPoint()
    {
        Vector2 randomOffset = Random.insideUnitCircle * driftDistance;
        driftTarget = rt.anchoredPosition + randomOffset;
    }

    void OnCloseClicked()
    {
        if (GameManager.Instance == null)
        {
            Destroy(gameObject);
            return;
        }

        switch (adType)
        {
            case AdType.Normal:
                GameManager.Instance.OnPopupClosed(this);
                break;

            case AdType.Bomb:
                GameManager.Instance.OnBombAdClicked(this);
                break;

            case AdType.Cascade:
                GameManager.Instance.OnCascadeAdClosed(this);
                break;
        }

        Destroy(gameObject);
    }

    void AutoDespawn()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnAdAutoDespawn(this);
        Destroy(gameObject);
    }
}
