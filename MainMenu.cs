using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject levelSelectPanel;

    [Header("Main Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button levelSelectButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button backButton;

    [Header("Level Buttons")]
    [SerializeField] private Button[] levelButtons;

    private void Awake()
    {
        // Unlock cursor immediately
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (PlayerPrefs.HasKey("MasterVolume"))
        {
            AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        }

        AutoFindButtonsIfMissing();
        WireAllButtons();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ShowMainPanel();
    }

    private void Update()
    {
        // Keep cursor unlocked and visible while on main menu
        if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void AutoFindButtonsIfMissing()
    {
        Button[] allButtons = GetComponentsInChildren<Button>(true);
        foreach (var btn in allButtons)
        {
            if (btn == null) continue;
            string n = btn.name.ToLower();

            if (playButton == null && n.Contains("play")) playButton = btn;
            else if (levelSelectButton == null && (n.Contains("levelselect") || n.Contains("select"))) levelSelectButton = btn;
            else if (quitButton == null && n.Contains("quit")) quitButton = btn;
            else if (backButton == null && n.Contains("back")) backButton = btn;
        }

        if (mainPanel == null)
        {
            Transform t = transform.Find("MainPanel");
            if (t != null) mainPanel = t.gameObject;
        }

        if (levelSelectPanel == null)
        {
            Transform t = transform.Find("LevelSelectPanel");
            if (t != null) levelSelectPanel = t.gameObject;
        }

        if (levelButtons == null || levelButtons.Length < 6)
        {
            levelButtons = new Button[6];
        }

        for (int i = 0; i < 6; i++)
        {
            if (levelButtons[i] == null)
            {
                int lvlNum = i + 1;
                foreach (var btn in allButtons)
                {
                    if (btn != null && btn.name.Equals("Btn_Level" + lvlNum, System.StringComparison.OrdinalIgnoreCase))
                    {
                        levelButtons[i] = btn;
                        break;
                    }
                }
            }
        }
    }

    private void WireAllButtons()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(() =>
            {
                PlayClickSound();
                PlayGame();
            });
        }

        if (levelSelectButton != null)
        {
            levelSelectButton.onClick.RemoveAllListeners();
            levelSelectButton.onClick.AddListener(() =>
            {
                PlayClickSound();
                OpenLevelSelect();
            });
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(() =>
            {
                PlayClickSound();
                QuitGame();
            });
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() =>
            {
                PlayClickSound();
                ShowMainPanel();
            });
        }

        if (levelButtons != null)
        {
            for (int i = 0; i < levelButtons.Length; i++)
            {
                if (levelButtons[i] != null)
                {
                    int levelIndex = i + 1;
                    levelButtons[i].onClick.RemoveAllListeners();
                    levelButtons[i].onClick.AddListener(() =>
                    {
                        PlayClickSound();
                        LoadLevel("Level" + levelIndex);
                    });
                }
            }
        }
    }

    public void PlayGame()
    {
        LoadLevel("Level1");
    }

    public void OpenLevelSelect()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(true);
    }

    public void ShowMainPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
    }

    public void LoadLevel(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void PlayClickSound()
    {
        int sampleRate = 44100;
        float duration = 0.08f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * 40f);
            float freq = 600f + (1f - (float)i / numSamples) * 400f;
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.4f;
        }

        AudioClip clip = AudioClip.Create("BtnClick", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);

        Camera cam = Camera.main;
        Vector3 pos = cam != null ? cam.transform.position + cam.transform.forward : Vector3.zero;
        AudioSource.PlayClipAtPoint(clip, pos, 0.8f);
    }
}
