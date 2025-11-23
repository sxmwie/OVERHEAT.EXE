using System.Collections;          // ← ADD THIS
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;


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
    [Range(0f, 1f)] public float jumpingChance   = 0.15f;

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
    public float tempOnAdClose = -2f; // matches "+2 cooling" text

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

    // ---------- POWERUP SETTINGS ----------
    [Header("Powerup Effects")]
    public float freezeDurationSeconds = 6f;
    public float coolAmount = 18f;
    public float clearAllTempBonus = 25f;   // how much clear-all cools (°C)

    bool tempFrozen = false;
    float freezeTimer = 0f;

    // ---------- POWERUP INVENTORY ----------
    [Header("Powerup Inventory")]
    public int invCool = 0;
    public int invFreeze = 0;
    public int invClear = 0;

    // these should point to the SMALL count texts next to your icons
    public TMP_Text invCoolText;    
    public TMP_Text invFreezeText;  
    public TMP_Text invClearText;   

    // ---------- HEAT VFX ----------
    [Header("Heat Visuals")]
    public Image heatOverlay;
    public RectTransform shakeRoot;
    [Range(0f, 1f)] public float shakeStartNormalized = 0.6f;
    public float shakeMaxAmplitude = 12f;
    [Range(0f, 1f)] public float overlayMaxAlpha = 0.45f;

    Vector2 shakeBasePos;

    // ---------- VISUAL FX ----------
    [Header("Visual FX")]
    public GameObject adCloseFxPrefab;        // square burst prefab
    public GameObject floatingTextPrefab;     // FloatingTextUI prefab

    [Header("Floating Text Strings")]
    public string adCoolingText       = "+2 cooling";
    public string powerupCoolingText  = "+10 cooling";
    public string powerupFreezeText   = "temperature freeze";
    public string powerupClearText    = "clean all";

    [Header("Floating Text Colors")]
    public Color adCoolingTextColor      = Color.cyan;
    public Color powerupCoolingColor     = Color.cyan;
    public Color powerupFreezeColor      = Color.blue;
    public Color powerupClearColor       = Color.yellow;

    // ---------- AUDIO ----------
    [Header("Audio")]
    [Tooltip("AudioSource with NO clip, PlayOnAwake OFF, Loop OFF")]
    public AudioSource virusSfxSource;

    [Tooltip("Clips used when a popup SPAWNS")]
    public List<AudioClip> virusSpawnClips;

    [Tooltip("Clips used when the player CLOSES a popup")]
    public List<AudioClip> adCloseClips;

    [Tooltip("AudioSource with pcFanLoop clip, PlayOnAwake ON, Loop ON")]
    public AudioSource fanSource;
    [Range(0f, 1f)] public float fanMinVolume = 0f;
    [Range(0f, 1f)] public float fanMaxVolume = 1f;
    //----- EXPLOSION -----


    [Header("Explosion FX")]

    [Tooltip("Full-screen Image with CanvasGroup, used for the explosion flash")]
    public CanvasGroup explosionOverlay;

    [Tooltip("Extra shake strength during explosion (multiplies shakeMaxAmplitude)")]
    public float explosionShakeMultiplier = 2f;

    [Tooltip("How long the bright flash lasts (seconds, unscaled time)")]
    public float explosionFlashDuration = 0.3f;

    [Tooltip("How long it takes for the flash to fade out and game over panel to fade in")]
    public float gameOverFadeDuration = 0.7f;  // (not used anymore, but kept)

    [Tooltip("How long to fade the fan volume to 0 after game over")]
    public float fanFadeOutDuration = 0.5f;

    [Tooltip("AudioSource for explosion SFX (one-shot)")]
    public AudioSource explosionSource;

    public AudioClip explosionClip;

    // ===== Fade to Black =====
    [Header("Fade to Black")]
    public CanvasGroup fadeToBlackOverlay;
    public float fadeToBlackDuration = 1.4f;


    // ---------- INTERNAL STATE ----------
    List<AdPopup> activeAds = new List<AdPopup>();
    List<GameObject> normalPrefabs  = new List<GameObject>();
    List<GameObject> bombPrefabs    = new List<GameObject>();
    List<GameObject> cascadePrefabs = new List<GameObject>();

    float elapsedTime = 0f;
    float spawnDifficultyTime = 0f;
    float spawnTimer = 0f;
    bool isGameOver = false;
    public bool IsGameOver => isGameOver;   // ← used by other scripts

    // ===== Game Over Fade =====
    [Header("Game Over Fade")]
    public float gameOverTextFadeDuration = 1.2f;

    // hover vars for game over text
    float gameOverHoverTimer = 0f;
    Vector2 gameOverBasePos;
    public float gameOverHoverAmplitude = 8f;
    public float gameOverHoverSpeed = 1.5f;


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

        // make sure explosion overlay starts hidden
        if (explosionOverlay != null)
        {
            explosionOverlay.alpha = 0f;
            explosionOverlay.gameObject.SetActive(false);
        }

        // make sure fade-to-black starts invisible but active
        if (fadeToBlackOverlay != null)
        {
            fadeToBlackOverlay.alpha = 0f;
            fadeToBlackOverlay.gameObject.SetActive(true);
        }

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
        UpdatePowerupUI();
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
        // when game over: only run hover
        if (isGameOver)
        {
            HoverGameOverText();
            return;
        }

        float dt = Time.deltaTime;
        elapsedTime += dt;
        spawnDifficultyTime += dt;

        if (timerText != null)
            timerText.text = "time: " + FormatTime(elapsedTime);

        HandleSpawning(dt);
        HandleTemperature(dt);
        HandlePowerupKeys();
    }

    // -------- hover animation for game over text --------
    void HoverGameOverText()
    {
        if (gameOverText == null) return;

        gameOverHoverTimer += Time.unscaledDeltaTime * gameOverHoverSpeed;
        float offsetY = Mathf.Sin(gameOverHoverTimer) * gameOverHoverAmplitude;

        RectTransform rt = gameOverText.rectTransform;
        rt.anchoredPosition = new Vector2(gameOverBasePos.x,
                                          gameOverBasePos.y + offsetY);
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

        // safenet: make sure popup fits inside area
        EnsurePopupFits(rt);

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
                if (Random.value < jumpingChance)
                    popup.jumping = true;
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

    public void EnsurePopupFits(RectTransform popupRt)
    {
        if (popupArea == null || popupRt == null) return;

        Rect parentRect = popupArea.rect;
        float maxWidth  = parentRect.width  - 2f * spawnPadding;
        float maxHeight = parentRect.height - 2f * spawnPadding;

        float w = popupRt.rect.width;
        float h = popupRt.rect.height;

        if (w <= maxWidth && h <= maxHeight)
            return;

        float scaleX = maxWidth  / w;
        float scaleY = maxHeight / h;

        float scale = Mathf.Min(scaleX, scaleY, 1f);
        popupRt.localScale *= scale;
    }

    // ---------- visual FX ----------

    public void SpawnAdCloseFx(RectTransform source)
    {
        if (adCloseFxPrefab == null || popupArea == null || source == null) return;

        GameObject fx = Instantiate(adCloseFxPrefab, popupArea);
        RectTransform fxRt = fx.GetComponent<RectTransform>();
        if (fxRt != null)
            fxRt.anchoredPosition = source.anchoredPosition;
    }

    public void SpawnFloatingText(string message, RectTransform source, Color color)
    {
        if (floatingTextPrefab == null || popupArea == null || source == null) return;

        GameObject go = Instantiate(floatingTextPrefab, popupArea);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = source.anchoredPosition;

        FloatingTextUI ft = go.GetComponent<FloatingTextUI>();
        if (ft != null)
            ft.Setup(message, color);
    }

    public void SpawnFloatingTextRandomInPopup(string message, Color color)
    {
        if (floatingTextPrefab == null || popupArea == null) return;

        GameObject go = Instantiate(floatingTextPrefab, popupArea);
        RectTransform rt = go.GetComponent<RectTransform>();

        Rect r = popupArea.rect;
        float x = Random.Range(-r.width * 0.5f,  r.width * 0.5f);
        float y = Random.Range(-r.height * 0.5f, r.height * 0.5f);
        rt.anchoredPosition = new Vector2(x, y);

        FloatingTextUI ft = go.GetComponent<FloatingTextUI>();
        if (ft != null)
            ft.Setup(message, color);
    }

    public void ShowAdCoolingText(RectTransform source)
    {
        SpawnFloatingText(adCoolingText, source, adCoolingTextColor);
    }

    public void ShowPowerupCoolingText()
    {
        SpawnFloatingTextRandomInPopup(powerupCoolingText, powerupCoolingColor);
    }

    public void ShowPowerupFreezeText()
    {
        SpawnFloatingTextRandomInPopup(powerupFreezeText, powerupFreezeColor);
    }

    public void ShowPowerupClearText()
    {
        SpawnFloatingTextRandomInPopup(powerupClearText, powerupClearColor);
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

    // ================== POWERUP INVENTORY ==================

    public void AddPowerupCool(int amount = 1)
    {
        invCool += amount;
        UpdatePowerupUI();
    }

    public void AddPowerupFreeze(int amount = 1)
    {
        invFreeze += amount;
        UpdatePowerupUI();
    }

    public void AddPowerupClear(int amount = 1)
    {
        invClear += amount;
        UpdatePowerupUI();
    }

    void UpdatePowerupUI()
    {
        // just show the counts next to icons
        if (invCoolText != null)
            invCoolText.text = invCool.ToString();

        if (invFreezeText != null)
            invFreezeText.text = invFreeze.ToString();

        if (invClearText != null)
            invClearText.text = invClear.ToString();
    }

    void HandlePowerupKeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            TryUseCool();

        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            TryUseFreeze();

        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            TryUseClear();
    }

    void TryUseCool()
    {
        if (isGameOver) return;
        if (invCool <= 0) return;

        invCool--;
        ApplyCoolPowerup(coolAmount);
        UpdatePowerupUI();
    }

    void TryUseFreeze()
    {
        if (isGameOver) return;
        if (invFreeze <= 0) return;

        invFreeze--;
        ApplyFreezePowerup(freezeDurationSeconds);
        UpdatePowerupUI();
    }

    void TryUseClear()
    {
        if (isGameOver) return;
        if (invClear <= 0) return;

        invClear--;
        ApplyClearAllPowerup();
        UpdatePowerupUI();
    }

    // ================== POWERUP EFFECTS (when actually USED) ==================

    public void ApplyFreezePowerup(float duration)
    {
        tempFrozen = true;
        freezeTimer = Mathf.Max(freezeTimer, duration);
        ShowPowerupFreezeText();
        UpdateTempUI();
    }

    public void ApplyCoolPowerup(float amount)
    {
        temp -= amount;
        temp = Mathf.Clamp(temp, 0f, tempMax);
        UpdateTempUI();
        ShowPowerupCoolingText();
    }

    public void ApplyClearAllPowerup()
    {
        // 1) wipe the screen
        foreach (var ad in activeAds)
        {
            if (ad != null)
                Destroy(ad.gameObject);
        }
        activeAds.Clear();

        // 2) COOL the PC a chunk (no spawn reset)
        if (clearAllTempBonus > 0f)
        {
            temp -= clearAllTempBonus;
            temp = Mathf.Clamp(temp, 0f, tempMax);
        }

        UpdateTempUI();
        ShowPowerupClearText();
    }

    // ================== GAME OVER ==================

    void TriggerGameOver(string reason)
    {
        if (isGameOver) return;
        isGameOver = true;   // lets other scripts stop immediately

        // update best time immediately
        float best = PlayerPrefs.GetFloat("BestTime", 0f);
        if (elapsedTime > best)
        {
            best = elapsedTime;
            PlayerPrefs.SetFloat("BestTime", best);
        }

        if (bestText != null)
            bestText.text = "best: " + FormatTime(best);

        // start the explosion + fade sequence
        StartCoroutine(GameOverSequence(reason));
    }

    IEnumerator GameOverSequence(string reason)
    {
        // 1) fade out the fan
        if (fanSource != null && fanFadeOutDuration > 0f)
        {
            StartCoroutine(FadeAudioOut(fanSource, fanFadeOutDuration));
        }

        // 2) explosion flash + heavy shake + explosion SFX
        if (explosionOverlay != null)
        {
            explosionOverlay.gameObject.SetActive(true);
            explosionOverlay.alpha = 1f;
        }

        if (explosionSource != null && explosionClip != null)
        {
            explosionSource.PlayOneShot(explosionClip);
        }

        float t = 0f;
        while (t < explosionFlashDuration)
        {
            t += Time.unscaledDeltaTime;

            // extra shake using existing shakeRoot
            if (shakeRoot != null)
            {
                float k = 1f - Mathf.Clamp01(t / explosionFlashDuration);
                float amp = shakeMaxAmplitude * explosionShakeMultiplier * k;
                Vector2 offset = Random.insideUnitCircle * amp;
                shakeRoot.anchoredPosition = shakeBasePos + offset;
            }

            yield return null;
        }

        // reset shake position
        if (shakeRoot != null)
            shakeRoot.anchoredPosition = shakeBasePos;

        // 3) fade from explosion white → full black
        t = 0f;
        while (t < fadeToBlackDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeToBlackDuration);

            if (explosionOverlay != null)
                explosionOverlay.alpha = 1f - k;

            if (fadeToBlackOverlay != null)
                fadeToBlackOverlay.alpha = k;

            yield return null;
        }

        if (explosionOverlay != null)
        {
            explosionOverlay.alpha = 0f;
            explosionOverlay.gameObject.SetActive(false);
        }

        // 4) now fade in game over panel on top of black
        CanvasGroup goCg = null;
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            goCg = gameOverPanel.GetComponent<CanvasGroup>();
            if (goCg == null) goCg = gameOverPanel.AddComponent<CanvasGroup>();
            goCg.alpha = 0f;
        }

        if (gameOverText != null)
        {
            gameOverText.text = reason + "\n\nsurvived: " + FormatTime(elapsedTime);
            // store base pos for hover
            gameOverBasePos = gameOverText.rectTransform.anchoredPosition;
            gameOverHoverTimer = 0f;
        }

        t = 0f;
        while (t < gameOverTextFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / gameOverTextFadeDuration);

            if (goCg != null)
                goCg.alpha = k;

            yield return null;
        }

        // 5) finally, hard freeze everything (hover uses unscaled time)
        Time.timeScale = 0f;
    }

    IEnumerator FadeAudioOut(AudioSource src, float duration)
    {
        if (src == null || duration <= 0f) yield break;

        float startVol = src.volume;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            src.volume = Mathf.Lerp(startVol, 0f, k);
            yield return null;
        }

        src.volume = 0f;
        src.Stop();
    }


    public void Restart()
    {
        Time.timeScale = 1f;
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
