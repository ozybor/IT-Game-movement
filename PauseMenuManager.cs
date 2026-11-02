using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Main Pause Buttons")]
    [SerializeField] private Button resumeBtn;
    [SerializeField] private Button restartBtn;
    [SerializeField] private Button optionsBtn;
    [SerializeField] private Button menuBtn;

    [Header("Options Controls")]
    [SerializeField] private Slider sensSlider;
    [SerializeField] private TextMeshProUGUI sensValueText;
    [SerializeField] private Slider volSlider;
    [SerializeField] private TextMeshProUGUI volValueText;
    [SerializeField] private Button backBtn;

    private bool isPaused = false;
    private AdvancedPlayerController player;
    private LevelCompleteUI completeUI;

    public bool IsPaused => isPaused;

    private const string PREF_SENS = "MouseSensitivity";
    private const string PREF_VOL = "MasterVolume";
    private const float DEFAULT_SENS = 0.12f;
    private const float DEFAULT_VOL = 1.0f;

    private void Awake()
    {
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        player = FindAnyObjectByType<AdvancedPlayerController>();
        completeUI = FindAnyObjectByType<LevelCompleteUI>();

        float savedVol = PlayerPrefs.GetFloat(PREF_VOL, DEFAULT_VOL);
        AudioListener.volume = savedVol;

        // Button events
        if (resumeBtn != null) resumeBtn.onClick.AddListener(ResumeGame);
        if (restartBtn != null) restartBtn.onClick.AddListener(RestartLevel);
        if (optionsBtn != null) optionsBtn.onClick.AddListener(ShowOptions);
        if (menuBtn != null) menuBtn.onClick.AddListener(GoToMainMenu);
        if (backBtn != null) backBtn.onClick.AddListener(HideOptions);

        // Slider events
        if (sensSlider != null)
        {
            sensSlider.minValue = 0.02f;
            sensSlider.maxValue = 0.50f;
            float savedSens = PlayerPrefs.GetFloat(PREF_SENS, DEFAULT_SENS);
            sensSlider.value = savedSens;
            sensSlider.onValueChanged.AddListener(OnSensitivityChanged);
            UpdateSensitivityDisplay(savedSens);
        }

        if (volSlider != null)
        {
            volSlider.minValue = 0f;
            volSlider.maxValue = 1f;
            volSlider.value = savedVol;
            volSlider.onValueChanged.AddListener(OnVolumeChanged);
            UpdateVolumeDisplay(savedVol);
        }

        if (pausePanel != null) pausePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoEnsurePauseMenuInScene()
    {
        if (SceneManager.GetActiveScene().name == "MainMenu") return;

        if (FindAnyObjectByType<PauseMenuManager>() == null)
        {
            var prefab = Resources.Load<GameObject>("PauseMenuCanvas");
            if (prefab != null) Instantiate(prefab);
        }
    }

    private void Update()
    {
        if (completeUI == null)
            completeUI = FindAnyObjectByType<LevelCompleteUI>();

        if (completeUI != null && completeUI.IsCompleted)
            return;

        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null)
        {
            if (kb.tabKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame)
            {
                TogglePause();
            }
        }
    }

    public void TogglePause()
    {
        if (completeUI != null && completeUI.IsCompleted) return;

        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (player == null) player = FindAnyObjectByType<AdvancedPlayerController>();
        if (player != null) player.SetCursorLock(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
            Transform card = pausePanel.transform.Find("PauseCard");
            if (card != null) card.gameObject.SetActive(true);
        }
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);

        if (player == null) player = FindAnyObjectByType<AdvancedPlayerController>();
        if (player != null)
        {
            player.SetCursorLock(true);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void ShowOptions()
    {
        if (pausePanel != null)
        {
            Transform card = pausePanel.transform.Find("PauseCard");
            if (card != null) card.gameObject.SetActive(false);
        }
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    public void HideOptions()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (pausePanel != null)
        {
            Transform card = pausePanel.transform.Find("PauseCard");
            if (card != null) card.gameObject.SetActive(true);
        }
    }

    private void OnSensitivityChanged(float val)
    {
        PlayerPrefs.SetFloat(PREF_SENS, val);
        PlayerPrefs.Save();

        if (player == null) player = FindAnyObjectByType<AdvancedPlayerController>();
        if (player != null) player.LookSensitivity = val;

        UpdateSensitivityDisplay(val);
    }

    private void OnVolumeChanged(float val)
    {
        PlayerPrefs.SetFloat(PREF_VOL, val);
        PlayerPrefs.Save();
        AudioListener.volume = val;
        UpdateVolumeDisplay(val);
    }

    private void UpdateSensitivityDisplay(float val)
    {
        if (sensValueText != null) sensValueText.text = val.ToString("F2");
    }

    private void UpdateVolumeDisplay(float val)
    {
        if (volValueText != null) volValueText.text = Mathf.RoundToInt(val * 100f) + "%";
    }
}

