using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene")]
    [Tooltip("Name of your gameplay scene (as in Build Settings)")]
    public string gameSceneName = "MainScene";

    [Header("Panels")]
    public RectTransform menuPanel;         // your MenuPanel (Start/Instr/Credits/Quit + TITLE)
    public RectTransform instructionsPanel; // InstructionsPanel
    public RectTransform creditsPanel;      // CreditsPanel

    [Header("Slide Settings")]
    [Tooltip("How long the slide animation lasts")]
    public float slideDuration = 0.4f;

    [Tooltip("How far the menu slides left/right from the center")]
    public float horizontalOffset = 260f;

    [Tooltip("X position off to the right where side panels start/end")]
    public float offscreenRightX = 700f;

    CanvasGroup instrCG;
    CanvasGroup creditsCG;

    bool panelOpen = false;                // is an instructions/credits panel currently open
    RectTransform currentSidePanel = null;
    CanvasGroup currentSideCg = null;
    bool isTransitioning = false;

    void Start()
    {
        // set up canvas groups for fading
        if (instructionsPanel != null)
        {
            instrCG = instructionsPanel.GetComponent<CanvasGroup>();
            if (instrCG == null) instrCG = instructionsPanel.gameObject.AddComponent<CanvasGroup>();
        }

        if (creditsPanel != null)
        {
            creditsCG = creditsPanel.GetComponent<CanvasGroup>();
            if (creditsCG == null) creditsCG = creditsPanel.gameObject.AddComponent<CanvasGroup>();
        }

        ShowMainMenuImmediate();
    }

    // put everything back to "just buttons in the center"
    void ShowMainMenuImmediate()
    {
        if (menuPanel != null)
            menuPanel.anchoredPosition = Vector2.zero;

        if (instructionsPanel != null)
        {
            instructionsPanel.gameObject.SetActive(false);
            instructionsPanel.anchoredPosition = new Vector2(offscreenRightX, 0f);
            if (instrCG != null) instrCG.alpha = 0f;
        }

        if (creditsPanel != null)
        {
            creditsPanel.gameObject.SetActive(false);
            creditsPanel.anchoredPosition = new Vector2(offscreenRightX, 0f);
            if (creditsCG != null) creditsCG.alpha = 0f;
        }

        panelOpen = false;
        currentSidePanel = null;
        currentSideCg = null;
        isTransitioning = false;
    }

    // ------------- BUTTON HANDLERS -------------

    public void OnStartClicked()
    {
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogError("MainMenuController: gameSceneName is empty.");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnInstructionsClicked()
    {
        if (panelOpen || isTransitioning || instructionsPanel == null) return;
        StartCoroutine(OpenSidePanel(instructionsPanel, instrCG));
    }

    public void OnCreditsClicked()
    {
        if (panelOpen || isTransitioning || creditsPanel == null) return;
        StartCoroutine(OpenSidePanel(creditsPanel, creditsCG));
    }

    public void OnBackToMenuClicked()
    {
        if (!panelOpen || isTransitioning || currentSidePanel == null) return;
        StartCoroutine(CloseCurrentSidePanel());
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ------------- COROUTINES -------------

    IEnumerator OpenSidePanel(RectTransform sidePanel, CanvasGroup sideCg)
    {
        isTransitioning = true;
        panelOpen = true;
        currentSidePanel = sidePanel;
        currentSideCg = sideCg;

        Vector2 center = Vector2.zero;
        Vector2 leftPos  = new Vector2(-horizontalOffset, 0f);   // where menu slides to
        Vector2 rightPos = new Vector2(horizontalOffset, 0f);    // where side panel ends up
        Vector2 startSidePos = new Vector2(offscreenRightX, 0f); // off-screen start

        sidePanel.gameObject.SetActive(true);
        sidePanel.anchoredPosition = startSidePos;
        if (sideCg != null) sideCg.alpha = 0f;

        float t = 0f;
        while (t < slideDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / slideDuration);
            // smoothstep
            k = k * k * (3f - 2f * k);

            if (menuPanel != null)
                menuPanel.anchoredPosition = Vector2.Lerp(center, leftPos, k);

            if (sidePanel != null)
                sidePanel.anchoredPosition = Vector2.Lerp(startSidePos, rightPos, k);

            if (sideCg != null)
                sideCg.alpha = Mathf.Lerp(0f, 1f, k);

            yield return null;
        }

        if (menuPanel != null) menuPanel.anchoredPosition = leftPos;
        if (sidePanel != null) sidePanel.anchoredPosition = rightPos;
        if (sideCg != null) sideCg.alpha = 1f;

        isTransitioning = false;
    }

    IEnumerator CloseCurrentSidePanel()
    {
        isTransitioning = true;

        RectTransform sidePanel = currentSidePanel;
        CanvasGroup sideCg = currentSideCg;

        Vector2 center = Vector2.zero;
        Vector2 leftPos  = new Vector2(-horizontalOffset, 0f);
        Vector2 rightPos = new Vector2(horizontalOffset, 0f);
        Vector2 endSidePos = new Vector2(offscreenRightX, 0f);

        float t = 0f;
        while (t < slideDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / slideDuration);
            k = k * k * (3f - 2f * k);

            if (menuPanel != null)
                menuPanel.anchoredPosition = Vector2.Lerp(leftPos, center, k);

            if (sidePanel != null)
                sidePanel.anchoredPosition = Vector2.Lerp(rightPos, endSidePos, k);

            if (sideCg != null)
                sideCg.alpha = Mathf.Lerp(1f, 0f, k);

            yield return null;
        }

        if (menuPanel != null) menuPanel.anchoredPosition = center;

        if (sidePanel != null)
        {
            sidePanel.anchoredPosition = endSidePos;
            sidePanel.gameObject.SetActive(false);
        }

        if (sideCg != null) sideCg.alpha = 0f;

        panelOpen = false;
        currentSidePanel = null;
        currentSideCg = null;
        isTransitioning = false;
    }
}
