using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Names")]
    [Tooltip("Exact name of your gameplay scene (check File → Build Settings)")]
    public string gameSceneName = "GameScene";

    [Header("Panels")]
    public GameObject menuPanel;
    public GameObject instructionsPanel;
    public GameObject creditsPanel;

    void Start()
    {
        // make sure we start in the main menu view
        ShowMainMenu();
    }

    // ---------- BUTTON HANDLERS ----------

    public void OnStartClicked()
    {
        // load the game scene
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            // ensure timeScale is normal in case we come from a paused state
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError("MainMenuController: gameSceneName is empty!");
        }
    }

    public void OnInstructionsClicked()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (instructionsPanel != null) instructionsPanel.SetActive(true);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    public void OnCreditsClicked()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (instructionsPanel != null) instructionsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    public void OnBackToMenuClicked()
    {
        ShowMainMenu();
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        // stops play mode in editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // quits the app in build
        Application.Quit();
#endif
    }

    // ---------- HELPER ----------

    void ShowMainMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(true);
        if (instructionsPanel != null) instructionsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }
}
