using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameSceneSetting : MonoBehaviour
{
    [Header("Pause Menu")]
    public GameObject pauseMenuObject;

    [Header("Pause Menu Buttons")]
    public Button settingsButton;
    public Button mainMenuButton;

    [Header("Settings Panel")]
    public GameObject settingsPanel;
    public TMP_Dropdown windowModeDropdown;
    public TMP_Dropdown resolutionDropdown;

    public Button settingsBackButton;

    [Header("Scene")]
    public string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;
    private Resolution[] availableResolutions;

    private FullScreenMode currentFullscreenMode = FullScreenMode.FullScreenWindow;

    private void Start()
    {
        if (pauseMenuObject != null) pauseMenuObject.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);
        if (settingsBackButton != null) settingsBackButton.onClick.AddListener(CloseSettings);

        PopulateWindowModeDropdown();
        PopulateResolutionDropdown();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettings();
            }
            else
            {
                TogglePauseMenu();
            }
        }
    }

    private void TogglePauseMenu()
    {
        isPaused = !isPaused;

        if (pauseMenuObject != null)
            pauseMenuObject.SetActive(isPaused);

        Time.timeScale = isPaused ? 0f : 1f;

        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isPaused;
    }

    private void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    private void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void PopulateWindowModeDropdown()
    {
        if (windowModeDropdown == null) return;

        windowModeDropdown.ClearOptions();

        var options = new List<string>
        {
            "Fullscreen",
            "Fullscreen Window",
            "Maximized Window",
            "Windowed"
        };

        windowModeDropdown.AddOptions(options);

        windowModeDropdown.value = FullscreenModeToIndex(Screen.fullScreenMode);
        windowModeDropdown.RefreshShownValue();

        windowModeDropdown.onValueChanged.AddListener(OnWindowModeChanged);
    }

    private void OnWindowModeChanged(int index)
    {
        currentFullscreenMode = IndexToFullscreenMode(index);
        Screen.fullScreenMode = currentFullscreenMode;
    }

    private static int FullscreenModeToIndex(FullScreenMode mode)
    {
        switch (mode)
        {
            case FullScreenMode.ExclusiveFullScreen: return 0;
            case FullScreenMode.FullScreenWindow: return 1;
            case FullScreenMode.MaximizedWindow: return 2;
            case FullScreenMode.Windowed: return 3;
            default: return 1;
        }
    }

    private static FullScreenMode IndexToFullscreenMode(int index)
    {
        switch (index)
        {
            case 0: return FullScreenMode.ExclusiveFullScreen;
            case 1: return FullScreenMode.FullScreenWindow;
            case 2: return FullScreenMode.MaximizedWindow;
            case 3: return FullScreenMode.Windowed;
            default: return FullScreenMode.FullScreenWindow;
        }
    }

    private void PopulateResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        resolutionDropdown.ClearOptions();

        availableResolutions = Screen.resolutions;

        var options = new List<string>();
        int currentIndex = 0;

        for (int i = 0; i < availableResolutions.Length; i++)
        {
            Resolution r = availableResolutions[i];

#if UNITY_2022_2_OR_NEWER
            string label = $"{r.width} x {r.height}  @{r.refreshRateRatio.value:F0} Hz";
#else
            string label = $"{r.width} x {r.height}  @{r.refreshRate} Hz";
#endif
            options.Add(label);

            if (r.width == Screen.currentResolution.width &&
                r.height == Screen.currentResolution.height)
            {
                currentIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentIndex;
        resolutionDropdown.RefreshShownValue();

        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    private void OnResolutionChanged(int index)
    {
        if (availableResolutions == null || index >= availableResolutions.Length) return;

        Resolution chosen = availableResolutions[index];
        Screen.SetResolution(chosen.width, chosen.height, Screen.fullScreenMode);
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}

