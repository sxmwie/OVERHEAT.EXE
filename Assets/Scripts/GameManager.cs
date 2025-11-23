using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ---------- UI AREAS ----------
    [Header("UI Areas")]
    public RectTransform popupArea;
    public RectTransform canvasRect;

    // ---------- AD PREFABS ----------
    [Header("Ad Prefabs")]
    public List<GameObject> adPrefabs = new List<GameObject>();

    // ---------- TIMER UI ----------
    [Header("Timer UI")]
    public TMP_Text timerText;
    public TMP_Text bestText;

    // ---------- TEMPERATURE ----------
    [Header("Temperature (°C internal)")]
    public float temp = 30f;
    public float tempMax = 100f;
    public float baseTempIncreasePerSecond = 1.5f;
    public float tempPerOpenAd = 0.15f;
    public float tempOnAdClose = -1f;

    [Header("Temperature UI")]
    public TemperatureBar tempBar;
    public TMP_Text tempValueText;

    // ---------- SPAWNING ----------
    [Header("Spawning")]
    public float baseSpawnInterval = 1.2f;
    public float minSpawnInterval = 0.6f;
    public float difficultyRampPerSecond = 0.004f;
    public float spawnPadding = 10f;

    // ---------- MOVING ----------

    [Header("Moving Ads")]
    [Range(0f, 1f)] public float movingAdChance = 0.15f; // 15% of normal ads move

    [Header("Ad Behaviours")]
    [Range(0f, 1f)] public float movingChance = 0.18f;
    [Range(0f, 1f)] public float breathingChance = 0.25f;
    [Range(0f, 1f)] public float rotatingChance = 0.18f;
    [Range(0f, 1f)] public float runAwayChance = 0.10f;



    // ---------- GAME OVER ----------
    [Header("Game Over")]
    public GameObject gameOverPanel;
    public TMP_Text gameOverText;

    // ---------- POWERUPS ----------
    [Header("Powerup Settings")]
    public float freezeDurationSeconds = 6f;
    public float coolAmount = 18f;       // °C reduction
    public float clearAllTempBonus = 0f; // optional cooling when clearing all

    bool tempFrozen = false;
    float freezeTimer = 0f;

    // ---------- HEAT VFX ----------
    [Header("Heat Visuals")]
    public Image heatOverlay;           // red full-screen image
    public RectTransform shakeRoot;     // UIRoot
    [Range(0f, 1f)] public float shakeStartNormalized = 0.6f;
    public float shakeMaxAmplitude = 12f;
    [Range(0f, 1f)] public float overlayMaxAlpha = 0.45f;

    Vector2 shakeBasePos;

    // ---------- AUDIO ----------
    [Header("Audio")]
    [Tooltip("AudioSource with NO clip, PlayOnAwake OFF, Loop OFF")]
    public AudioSource virusSfxSource;          // plays one-shot virus sounds
    [Tooltip("3+ different virus spawn clips")]
    public List<AudioClip> virusSpawnClips;     // random per spawn

    [Tooltip("AudioSource with pcFanLoop clip, PlayOnAwake ON, Loop ON")]
    public AudioSource fanSource;               // looping fan hum
    [Range(0f, 1f)] public float fanMinVolume = 0f;
    [Range(0f, 1f)] public float fanMaxVolume = 1f;

    // ---------- INTERNAL STATE ----------
    List<AdPopup> activeAds = new List<AdPopup>();
    float elapsedTime = 0f;
    float spawnDifficultyTime = 0f;
    float spawnTimer = 0f;
    bool isGameOver = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (canvasRect == null)
        {
            Canvas c = FindObjectOfType<Canvas>();
            if (c != null) canvasRect = c.GetComponent<RectTransform>();
        }

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        float best = PlayerPrefs.GetFloat("BestTime", 0f);
        if (bestText != null)
            bestText.text = "best: " + FormatTime(best);

        // base position for shaking
        if (shakeRoot != null)
            shakeBasePos = shakeRoot.anchoredPosition;

        // fan setup
        if (fanSource != null)
        {
            if (fanSource.clip != null && !fanSource.isPlaying)
                fanSource.Play();
            fanSource.volume = fanMinVolume;
        }

        UpdateTempUI();
    }

    void Update()
    {
        if (isGameOver) return;

        float dt = Time.deltaTime;
        elapsedTime += dt;
        spawnDifficultyTime += dt;

        if (timerText != null)
            timerText.text = "time: " + FormatTime(elapsedTime);

        HandleSpawning(dt);
        HandleTemperature(dt);
    }

    // ================== SPAWNING ==================

    void HandleSpawning(float dt)
    {
        if (adPrefabs == null || adPrefabs.Count == 0 || popupArea == null) return;

        float currentInterval = Mathf.Max(
            minSpawnInterval,
            baseSpawnInterval - difficultyRampPerSecond * spawnDifficultyTime
        );

        spawnTimer += dt;
        if (spawnTimer >= currentInterval)
        {
            spawnTimer = 0f;
            SpawnPopup();     // generic spawn (can be normal / bomb / cascade)
        }
    }

    void SpawnPopup()
    {
        if (adPrefabs.Count == 0) return;

        int index = Random.Range(0, adPrefabs.Count);
        GameObject prefab = adPrefabs[index];

        SpawnPopupFromPrefab(prefab);
    }

    // common instantiate logic
    void SpawnPopupFromPrefab(GameObject prefab)
    {
        GameObject go = Instantiate(prefab, popupArea);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.localScale = Vector3.one;

        Vector2 pos = RandomInsidePopupArea(rt);
        rt.anchoredPosition = pos;

        AdPopup popup = go.GetComponent<AdPopup>();

        // apply extra behaviours only to NORMAL ads
        if (popup != null && popup.adType == AdPopup.AdType.Normal)
        {
            if (Random.value < movingChance)
                popup.isMovingAd = true;

            if (Random.value < breathingChance)
                popup.breathing = true;

            if (Random.value < rotatingChance)
                popup.rotating = true;

            if (Random.value < runAwayChance)
                popup.runFromCursor = true;
        }


        // randomly assign movement to normal ads only
        if (popup != null && popup.adType == AdPopup.AdType.Normal)
        {
            float roll = Random.value; // 0–1
            if (roll < movingAdChance)      // if chance succeeds, set as moving ad
                popup.isMovingAd = true;
        }

        if (popup != null)
            activeAds.Add(popup);

        PlayVirusSpawnSfx();
    }

    // spawn N normal ads (used by cascade)
    public void SpawnMultipleNormalAds(int count)
    {
        List<GameObject> normalPrefabs = new List<GameObject>();

        foreach (var prefab in adPrefabs)
        {
            AdPopup p = prefab.GetComponent<AdPopup>();
            if (p != null && p.adType == AdPopup.AdType.Normal)
                normalPrefabs.Add(prefab);
        }

        if (normalPrefabs.Count == 0)
        {
            // fallback: just use all
            for (int i = 0; i < count; i++)
                SpawnPopup();
            return;
        }

        for (int i = 0; i < count; i++)
        {
            GameObject choice = normalPrefabs[Random.Range(0, normalPrefabs.Count)];
            SpawnPopupFromPrefab(choice);
        }
    }

    Vector2 RandomInsidePopupArea(RectTransform popupRt)
    {
        Rect areaRect = popupArea.rect;
        float parentWidth = areaRect.width;
        float parentHeight = areaRect.height;

        float halfW = popupRt.rect.width / 2f;
        float halfH = popupRt.rect.height / 2f;

        float maxX = parentWidth / 2f - halfW - spawnPadding;
        float maxY = parentHeight / 2f - halfH - spawnPadding;

        float x = Random.Range(-maxX, maxX);
        float y = Random.Range(-maxY, maxY);

        return new Vector2(x, y);
    }

    public void SnapPopupInside(RectTransform popupRt)
    {
        Rect areaRect = popupArea.rect;
        float parentWidth = areaRect.width;
        float parentHeight = areaRect.height;

        float halfW = popupRt.rect.width / 2f;
        float halfH = popupRt.rect.height / 2f;

        float maxX = parentWidth / 2f - halfW - spawnPadding;
        float maxY = parentHeight / 2f - halfH - spawnPadding;

        Vector2 pos = popupRt.anchoredPosition;
        pos.x = Mathf.Clamp(pos.x, -maxX, maxX);
        pos.y = Mathf.Clamp(pos.y, -maxY, maxY);
        popupRt.anchoredPosition = pos;
    }

    // ================== POPUP CALLBACKS ==================

    // normal safe ad closed by player
    public void OnPopupClosed(AdPopup popup)
    {
        if (activeAds.Contains(popup))
            activeAds.Remove(popup);

        temp += tempOnAdClose;              // cooling reward
        temp = Mathf.Clamp(temp, 0f, tempMax);
        UpdateTempUI();
    }

    // bomb ad clicked by player
    public void OnBombAdClicked(AdPopup popup)
    {
        if (activeAds.Contains(popup))
            activeAds.Remove(popup);

        TriggerGameOver("you clicked a malware bomb ad!");
    }

    // cascade ad closed by player
    public void OnCascadeAdClosed(AdPopup popup)
    {
        if (activeAds.Contains(popup))
            activeAds.Remove(popup);

        // still give some cooling for closing it
        temp += tempOnAdClose;
        temp = Mathf.Clamp(temp, 0f, tempMax);
        UpdateTempUI();

        int extraCount = Random.Range(5, 16);   // 5 to 15
        SpawnMultipleNormalAds(extraCount);
    }

    // auto-despawned (bomb/cascade you avoided)
    public void OnAdAutoDespawn(AdPopup popup)
    {
        if (activeAds.Contains(popup))
            activeAds.Remove(popup);
        // no temp change
    }

    // ================== TEMPERATURE ==================

    void HandleTemperature(float dt)
    {
        if (tempFrozen)
        {
            freezeTimer -= dt;
            if (freezeTimer <= 0f)
            {
                freezeTimer = 0f;
                tempFrozen = false;
            }

            UpdateTempUI();
            return;
        }

        float inc = baseTempIncreasePerSecond * dt;
        inc += activeAds.Count * tempPerOpenAd * dt;

        temp += inc;
        temp = Mathf.Clamp(temp, 0f, tempMax);

        UpdateTempUI();

        if (temp >= tempMax)
        {
            TriggerGameOver("your pc overheated!");
        }
    }

    void UpdateTempUI()
    {
        float tNorm = Mathf.Clamp01(temp / tempMax);

        if (tempBar != null)
            tempBar.SetValue01(tNorm);

        if (tempValueText != null)
        {
            float f = temp * 9f / 5f + 32f;
            tempValueText.text = Mathf.RoundToInt(f) + "°F";
        }

        UpdateHeatVFX(tNorm);
    }

    void UpdateHeatVFX(float tNorm)
    {
        float heat01 = Mathf.InverseLerp(shakeStartNormalized, 1f, tNorm);
        heat01 = Mathf.Clamp01(heat01);

        // red tint
        if (heatOverlay != null)
        {
            Color c = heatOverlay.color;
            c.a = overlayMaxAlpha * heat01;
            heatOverlay.color = c;
        }

        // shake
        if (shakeRoot != null)
        {
            Vector2 randomOffset = Random.insideUnitCircle * (shakeMaxAmplitude * heat01);
            shakeRoot.anchoredPosition = shakeBasePos + randomOffset;
        }

        // fan volume
        UpdateFanVolume(heat01);
    }

    // ================== AUDIO HELPERS ==================

    void PlayVirusSpawnSfx()
    {
        if (virusSfxSource == null) return;
        if (virusSpawnClips == null || virusSpawnClips.Count == 0) return;

        int index = Random.Range(0, virusSpawnClips.Count);
        AudioClip chosen = virusSpawnClips[index];
        if (chosen != null)
            virusSfxSource.PlayOneShot(chosen);
    }

    void UpdateFanVolume(float heat01)
    {
        if (fanSource == null) return;
        fanSource.volume = Mathf.Lerp(fanMinVolume, fanMaxVolume, heat01);
    }

    // ================== POWERUP API ==================

    public void ApplyFreezePowerup(float duration)
    {
        tempFrozen = true;
        freezeTimer = Mathf.Max(freezeTimer, duration);
    }

    public void ApplyCoolPowerup(float amount)
    {
        temp -= amount;
        temp = Mathf.Clamp(temp, 0f, tempMax);
        UpdateTempUI();
    }

    public void ApplyClearAllPowerup()
    {
        foreach (var ad in activeAds)
        {
            if (ad != null)
                Destroy(ad.gameObject);
        }
        activeAds.Clear();

        spawnDifficultyTime = 0f;
        spawnTimer = 0f;

        if (clearAllTempBonus > 0f)
        {
            temp -= clearAllTempBonus;
            temp = Mathf.Clamp(temp, 0f, tempMax);
        }

        UpdateTempUI();
    }

    // ================== GAME OVER ==================

    void TriggerGameOver(string reason)
    {
        if (isGameOver) return;
        isGameOver = true;

        float best = PlayerPrefs.GetFloat("BestTime", 0f);
        if (elapsedTime > best)
        {
            best = elapsedTime;
            PlayerPrefs.SetFloat("BestTime", best);
        }

        if (bestText != null)
            bestText.text = "best: " + FormatTime(best);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (gameOverText != null)
            gameOverText.text = reason + "\n\nsurvived: " + FormatTime(elapsedTime);
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ================== UTILS ==================

    string FormatTime(float t)
    {
        int s = Mathf.FloorToInt(t);
        int m = s / 60;
        int sec = s % 60;
        return m.ToString("00") + ":" + sec.ToString("00");
    }
}
