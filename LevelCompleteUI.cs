using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LevelCompleteUI : MonoBehaviour
{
    [Header("UI Panels & Text")]
    [SerializeField] private GameObject completionPanel;
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI nextButtonText;

    [Header("Buttons")]
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Level Info")]
    [SerializeField] private string nextSceneName = "Level2";
    [SerializeField] private bool isFinalLevel = false;

    private float levelStartTime;
    private bool isCompleted = false;

    public bool IsCompleted => isCompleted;

    private void Awake()
    {
        levelStartTime = Time.time;
        if (completionPanel != null)
        {
            completionPanel.SetActive(false);
        }

        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(OnNextLevelClicked);
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }

    private void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
        {
            OnRestartClicked();
        }
    }

    public void ShowCompletion()
    {
        if (isCompleted) return;
        isCompleted = true;

        float timeElapsed = Time.time - levelStartTime;
        int minutes = Mathf.FloorToInt(timeElapsed / 60f);
        float seconds = timeElapsed % 60f;
        string timeFormatted = string.Format("{0:00}:{1:00.00}", minutes, seconds);

        // Disable player controller & release cursor
        var player = FindAnyObjectByType<AdvancedPlayerController>();
        if (player != null)
        {
            player.SetCursorLock(false);
            player.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (isFinalLevel)
        {
            if (headerText != null) headerText.text = "MISSION COMPLETE!";
            if (subtitleText != null) subtitleText.text = "You collected all 6 documents!";
            if (nextButtonText != null) nextButtonText.text = "Main Menu";
        }
        else
        {
            if (headerText != null) headerText.text = "LEVEL COMPLETE!";
            if (subtitleText != null) subtitleText.text = "Document Collected!";
            if (nextButtonText != null) nextButtonText.text = "Next Level >";
        }

        if (statsText != null)
        {
            statsText.text = "Time: " + timeFormatted;
        }

        if (completionPanel != null)
        {
            completionPanel.SetActive(true);
        }
    }

    public void OnNextLevelClicked()
    {
        Time.timeScale = 1f;
        if (isFinalLevel || string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene("MainMenu");
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    public void OnRestartClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnMainMenuClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void SetNextScene(string nextScene, bool finalLevel)
    {
        nextSceneName = nextScene;
        isFinalLevel = finalLevel;
    }
}
