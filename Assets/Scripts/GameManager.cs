using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;   // always use Unity's Random

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

    [Header("Ad Rarity (Type Weights)")]
    [Range(0f, 5f)] public float normalWeight  = 1f;
    [Range(0f, 5f)] public float bombWeight    = 0.25f;
    [Range(0f, 5f)] public float cascadeWeight = 0.2f;

    [Header("Ad Behaviours (for NORMAL ads)")]
    [Range(0f, 1f)] public float movingChance    = 0.18f;
    [Range(0f, 1f)] public float breathingChance = 0.30f;
    [Range(0f, 1f)] public float rotatingChance  = 0.18f;
    [Range(0f, 1f)] public float runAwayChance   = 0.10f;

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

    // ---------- GAME OVER ----------
    [Header("Game Over")]
    public GameObject gameOverPanel;
    public TMP_Text gameOverText;

    // ---------- POWERUPS ----------
    [Header("Powerup Settings")]
    public float freezeDurationSeconds = 6f;
    public float coolAmount = 18f;
    public float clearAllTempBonus = 0f;

    bool tempFrozen = false;
    float freezeTimer = 0f;

    // ---------- HEAT VFX ----------
    [Header("Heat Visuals")]
    public Image heatOverlay;
    public RectTransform shakeRoot;
    [Range(0f, 1f)] public float shakeStartNormalized = 0.6f;
    public float shakeMaxAmplitude = 12f;
    [Range(0f, 1f)] public float overlayMaxAlpha = 0.45f;

    Vector2 shakeBasePos;

    // ---------- AUDIO ----------
    [Header("Audio")]
    [Tooltip("AudioSource with NO clip, PlayOnAwake OFF, Loop OFF")]
    public AudioSource virusSfxSource;               // shared one-shot source

    [Tooltip("Clips used when a popup SPAWNS")]
    public List<AudioClip> virusSpawnClips;

    [Tooltip("Clips used when the player CLOSES a popup")]
    public List<AudioClip> adCloseClips;

    [Tooltip("AudioSource with pcFanLoop clip, PlayOnAwake ON, Loop ON")]
    public AudioSource fanSource;
    [Range(0f, 1f)] public float fanMinVolume = 0f;
    [Range(0f, 1f)] public float fanMaxVolume = 1f;

    // ---------- INTERNAL STATE ----------
    List<AdPopup> activeAds = new List<AdPopup>();
    List<GameObject> normalPrefabs  = new List<GameObject>();
    List<GameObject> bombPrefabs    = new List<GameObject>();
    List<GameObject> cascadePrefabs = new List<GameObject>();

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

        if (shakeRoot != null)
            shakeBasePos = shakeRoot.anchoredPosition;

        if (fanSource != null)
        {
            if (fanSource.clip != null && !fanSource.isPlaying)
                fanSource.Play();
            fanSource.volume = fanMinVolume;
        }

        RebuildAdTypeLists();
        UpdateTempUI();
    }

    void RebuildAdTypeLists()
    {
        normalPrefabs.Clear();
        bombPrefabs.Clear();
        cascadePrefabs.Clear();

        foreach (var prefab in adPrefabs)
        {
            if (prefab == null) continue;
            AdPopup ap = prefab.GetComponent<AdPopup>();
            if (ap == null) continue;

            switch (ap.adType)
            {
                case AdPopup.AdType.Normal:
                    normalPrefabs.Add(prefab);
                    break;
                case AdPopup.AdType.Bomb:
                    bombPrefabs.Add(prefab);
                    break;
                case AdPopup.AdType.Cascade:
                    cascadePrefabs.Add(prefab);
                    break;
            }
        }
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
            SpawnPopup();
        }
    }

    void SpawnPopup()
    {
        GameObject prefab = PickAdPrefabByRarity();
        if (prefab == null)
        {
            if (adPrefabs.Count == 0) return;
            prefab = adPrefabs[Random.Range(0, adPrefabs.Count)];
        }

        SpawnPopupFromPrefab(prefab);
    }

    GameObject PickAdPrefabByRarity()
    {
        float nW = (normalPrefabs.Count  > 0) ? Mathf.Max(0f, normalWeight)  : 0f;
        float bW = (bombPrefabs.Count    > 0) ? Mathf.Max(0f, bombWeight)    : 0f;
        float cW = (cascadePrefabs.Count > 0) ? Mathf.Max(0f, cascadeWeight) : 0f;

        float total = nW + bW + cW;
        if (total <= 0.0001f) return null;

        float r = Random.value * total;

        if (r < nW && normalPrefabs.Count > 0)
            return normalPrefabs[Random.Range(0, normalPrefabs.Count)];
        r -= nW;

        if (r < bW && bombPrefabs.Count > 0)
            return bombPrefabs[Random.Range(0, bombPrefabs.Count)];
        r -= bW;

        if (cascadePrefabs.Count > 0)
            return cascadePrefabs[Random.Range(0, cascadePrefabs.Count)];

        return null;
    }

    void SpawnPopupFromPrefab(GameObject prefab)
    {
        GameObject go = Instantiate(prefab, popupArea);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.localScale = Vector3.one;

        Vector2 pos = RandomInsidePopupArea(rt);
        rt.anchoredPosition = pos;

        AdPopup popup = go.GetComponent<AdPopup>();
        if (popup != null)
        {
            if (popup.adType == AdPopup.AdType.Normal)
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

            activeAds.Add(popup);
        }

        PlayVirusSpawnSfx();
    }

    public void SpawnMultipleNormalAds(int count)
    {
        if (normalPrefabs.Count == 0)
        {
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
        float parentWidth  = popupArea.rect.width;
        float parentHeight = popupArea.rect.height;

        float halfW = popupRt.rect.width / 2f;
        float halfH = popupRt.rect.height / 2f;

        float maxX = parentWidth  / 2f - halfW - spawnPadding;
        float maxY = parentHeight / 2f - halfH - spawnPadding;

        float x = Random.Range(-maxX, maxX);
        float y = Random.Range(-maxY, maxY);

        return new Vector2(x, y);
    }

    public void SnapPopupInside(RectTransform popupRt)
    {
        float parentWidth  = popupArea.rect.width;
        float parentHeight = popupArea.rect.height;

        float halfW = popupRt.rect.width / 2f;
        float halfH = popupRt.rect.height / 2f;

        float maxX = parentWidth  / 2f - halfW - spawnPadding;
        float maxY = parentHeight / 2f - halfH - spawnPadding;

        Vector2 pos = popupRt.anchoredPosition;
        pos.x = Mathf.Clamp(pos.x, -maxX, maxX);
        pos.y = Mathf.Clamp(pos.y, -maxY, maxY);
        popupRt.anchoredPosition = pos;
    }

    // ================== POPUP CALLBACKS ==================

    public void OnPopupClosed(AdPopup popup)
    {
        if (activeAds.Contains(popup))
            activeAds.Remove(popup);

        temp += tempOnAdClose;
        temp = Mathf.Clamp(temp, 0f, tempMax);
        UpdateTempUI();
    }

    public void OnBombAdClicked(AdPopup popup)
    {
        if (activeAds.Contains(popup))
            activeAds.Remove(popup);

        TriggerGameOver("you clicked a malware bomb ad!");
    }

    public void OnCascadeAdClosed(AdPopup popup)
    {
        if (activeAds.Contains(popup))
            activeAds.Remove(popup);

        temp += tempOnAdClose;
        temp = Mathf.Clamp(temp, 0f, tempMax);
        UpdateTempUI();

        int extraCount = Random.Range(5, 16);
        SpawnMultipleNormalAds(extraCount);
    }

    public void OnAdAutoDespawn(AdPopup popup)
    {
        if (activeAds.Contains(popup))
            activeAds.Remove(popup);
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
            TriggerGameOver("your pc overheated!");
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

        if (heatOverlay != null)
        {
            Color c = heatOverlay.color;
            c.a = overlayMaxAlpha * heat01;
            heatOverlay.color = c;
        }

        if (shakeRoot != null)
        {
            Vector2 randomOffset = Random.insideUnitCircle * (shakeMaxAmplitude * heat01);
            shakeRoot.anchoredPosition = shakeBasePos + randomOffset;
        }

        UpdateFanVolume(heat01);
    }

    // ================== AUDIO ==================

    void PlayVirusSpawnSfx()
    {
        if (virusSfxSource == null) return;
        if (virusSpawnClips == null || virusSpawnClips.Count == 0) return;

        var clip = virusSpawnClips[Random.Range(0, virusSpawnClips.Count)];
        if (clip != null)
            virusSfxSource.PlayOneShot(clip);
    }

    public void PlayAdCloseSfx()
    {
        if (virusSfxSource == null) return;
        if (adCloseClips == null || adCloseClips.Count == 0) return;

        var clip = adCloseClips[Random.Range(0, adCloseClips.Count)];
        if (clip != null)
            virusSfxSource.PlayOneShot(clip);
    }

    void UpdateFanVolume(float heat01)
    {
        if (fanSource == null) return;
        fanSource.volume = Mathf.Lerp(fanMinVolume, fanMaxVolume, heat01);
    }

    // ================== POWERUPS ==================

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
            if (ad != null)
                Destroy(ad.gameObject);

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
