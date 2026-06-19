using UnityEngine;
using TMPro;

public class EvolutionTunerUI : MonoBehaviour
{
    private bool dropdownListenersWired = false;

    [Header("Wiring")]
    [Tooltip("Optional reference. If set, applying will also update the manager if it exists in this scene.")]
    public EvolutionManager evolutionManager;

    [Tooltip("Panel that contains the evolution UI controls (legacy). If you use evolutionPanel/settingsPanel, leave this null.")]
    public GameObject panel;

    [Header("Inputs (TMP)")]
    public TMP_InputField populationSizeInput;
    public TMP_InputField generationDurationInput;
    public TMP_InputField mutationRateInput;
    public TMP_InputField mutationStrengthInput;
    public TMP_InputField timeScaleInput;

    [Header("Display Settings (TMP)")]
    [Tooltip("Dropdown/selection that maps to Screen.fullScreenMode.")]
    public TMP_Dropdown fullScreenModeDropdown;

    [Tooltip("Dropdown of resolutions (e.g., 1280x720, 1920x1080).")]
    public TMP_Dropdown resolutionDropdown;

    [Header("Panel Control (optional)")]
    [Tooltip("If set, Evolution panel can be shown/hidden separately from other panels.")]
    public GameObject evolutionPanel;

    [Tooltip("If set, Display/Game settings panel can be shown/hidden separately from other panels.")]
    public GameObject settingsPanel;

    [Header("Optional")]
    public bool pauseTimeWhilePanelOpen = false;

    void Awake()
    {
        if (panel != null)
            panel.SetActive(false);

        EvolutionTunerSettings.resolutionWidth = Screen.currentResolution.width;
        EvolutionTunerSettings.resolutionHeight = Screen.currentResolution.height;
        EvolutionTunerSettings.fullScreenMode = (int)Screen.fullScreenMode;

        SyncFromSettingsToUI();
    }

    void Start()
    {
        if (evolutionManager != null)
            SyncFromManagerToUI();
        else
            SyncFromSettingsToUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (overallSettingsPanel != null && overallSettingsPanel.activeSelf)
            {
                HideOverallSettingsPanel();
            }
            else
            {
                if (evolutionPanel != null && evolutionPanel.activeSelf)
                    HideEvolutionPanel();
                else if (settingsPanel != null && settingsPanel.activeSelf)
                    HideSettingsPanel();
                else if (panel != null && panel.activeSelf)
                    TogglePanel();
            }
        }

        bool active = false;
        if (overallSettingsPanel != null && overallSettingsPanel.activeSelf)
            active = true;
        else if (evolutionPanel != null) active = evolutionPanel.activeSelf;
        else if (settingsPanel != null) active = settingsPanel.activeSelf;
        else if (panel != null) active = panel.activeSelf;

        if (active && Input.GetKeyDown(KeyCode.Return))
            ApplyUIToManager();
    }

    void TogglePanel()
    {
        if (panel == null) return;

        bool next = !panel.activeSelf;
        panel.SetActive(next);

        if (next)
        {
            SyncFromSettingsToUI();
            if (evolutionManager != null)
                SyncFromManagerToUI();
        }
    }

    [Header("Overall Settings Panel (optional)")]
    [Tooltip("If set, ShowOverallSettingsPanel will enable this and hide evolutionPanel/settingsPanel as appropriate.")]
    public GameObject overallSettingsPanel;

    public void ShowOverallSettingsPanel()
    {
        if (overallSettingsPanel != null)
            overallSettingsPanel.SetActive(true);
    }

    public void HideOverallSettingsPanel()
    {
        if (overallSettingsPanel != null)
            overallSettingsPanel.SetActive(false);

        HideEvolutionPanel();
        HideSettingsPanel();
    }

    public void ShowEvolutionPanel()
    {
        if (evolutionPanel == null) return;
        evolutionPanel.SetActive(true);
        SyncFromSettingsToUI();
        if (evolutionManager != null) SyncFromManagerToUI();
    }

    public void HideEvolutionPanel()
    {
        if (evolutionPanel == null) return;
        evolutionPanel.SetActive(false);
    }

    public void ShowSettingsPanel()
    {
        if (settingsPanel == null) return;
        settingsPanel.SetActive(true);
        SyncFromSettingsToUI();
    }

    public void HideSettingsPanel()
    {
        if (settingsPanel == null) return;
        settingsPanel.SetActive(false);
    }

    void SyncFromSettingsToUI()
    {
        if (populationSizeInput != null)
            populationSizeInput.text = EvolutionTunerSettings.populationSize.ToString();

        if (generationDurationInput != null)
            generationDurationInput.text = EvolutionTunerSettings.generationDuration.ToString("0.###");

        if (mutationRateInput != null)
            mutationRateInput.text = EvolutionTunerSettings.mutationRate.ToString("0.###");

        if (mutationStrengthInput != null)
            mutationStrengthInput.text = EvolutionTunerSettings.mutationStrength.ToString("0.###");

        if (timeScaleInput != null)
            timeScaleInput.text = EvolutionTunerSettings.timeScale.ToString("0.###");

        PopulateDropdowns();

        if (fullScreenModeDropdown != null)
        {
            int mode = EvolutionTunerSettings.fullScreenMode;
            mode = Mathf.Clamp(mode, 0, 3);

            if (mode == (int)UnityEngine.FullScreenMode.MaximizedWindow)
                mode = (int)UnityEngine.FullScreenMode.FullScreenWindow;

            fullScreenModeDropdown.value = Mathf.Clamp(mode, 0, 2);
        }

        if (resolutionDropdown != null && resolutionDropdown.options != null && resolutionDropdown.options.Count > 0)
        {
            string target = $"{EvolutionTunerSettings.resolutionWidth}x{EvolutionTunerSettings.resolutionHeight}";
            string targetNorm = target.Replace(" ", "");

            int found = -1;
            for (int i = 0; i < resolutionDropdown.options.Count; i++)
            {
                string opt = resolutionDropdown.options[i].text;
                if (opt != null)
                {
                    string optNorm = opt.Replace(" ", "");

                    int atIdx = optNorm.IndexOf('@');
                    if (atIdx >= 0)
                        optNorm = optNorm.Substring(0, atIdx);

                    if (optNorm.Equals(targetNorm, System.StringComparison.OrdinalIgnoreCase))
                    {
                        found = i;
                        break;
                    }
                }
            }

            if (found >= 0)
                resolutionDropdown.value = found;
        }
    }

    private void PopulateDropdowns()
    {
        PopulateFullScreenModeDropdownInternal();
        PopulateResolutionDropdownInternal();
    }

    private void PopulateFullScreenModeDropdownInternal()
    {
        if (fullScreenModeDropdown == null) return;

        fullScreenModeDropdown.ClearOptions();
        var options = new System.Collections.Generic.List<string>
        {
            "Fullscreen",
            "Borderless Fullscreen",
            "Windowed"
        };

        fullScreenModeDropdown.AddOptions(options);

        if (!dropdownListenersWired)
        {
            fullScreenModeDropdown.onValueChanged.AddListener(_ => { });
        }
    }

    private void PopulateResolutionDropdownInternal()
    {
        if (resolutionDropdown == null) return;

        resolutionDropdown.ClearOptions();

        var resolutions = Screen.resolutions;
        if (resolutions == null || resolutions.Length == 0) return;

        var seen = new System.Collections.Generic.HashSet<string>();
        var options = new System.Collections.Generic.List<string>();

        for (int i = 0; i < resolutions.Length; i++)
        {
            var r = resolutions[i];
            string key = $"{r.width}x{r.height}";
            if (!seen.Add(key))
                continue;

#if UNITY_2022_2_OR_NEWER
            string label = $"{r.width} x {r.height} @{r.refreshRateRatio.value:F0} Hz";
#else
            string label = $"{r.width} x {r.height} @{r.refreshRate} Hz";
#endif
            options.Add(label);
        }

        if (options.Count == 0)
        {
            for (int i = 0; i < resolutions.Length; i++)
            {
                var r = resolutions[i];
#if UNITY_2022_2_OR_NEWER
                string label = $"{r.width} x {r.height} @{r.refreshRateRatio.value:F0} Hz";
#else
                string label = $"{r.width} x {r.height} @{r.refreshRate} Hz";
#endif
                options.Add(label);
            }
        }

        resolutionDropdown.AddOptions(options);

        if (!dropdownListenersWired)
        {
            resolutionDropdown.onValueChanged.AddListener(_ => { });
            dropdownListenersWired = true;
        }
    }

    public void SyncFromManagerToUI()
    {
        if (evolutionManager == null) return;

        if (populationSizeInput != null)
            populationSizeInput.text = evolutionManager.populationSize.ToString();

        if (generationDurationInput != null)
            generationDurationInput.text = evolutionManager.generationDuration.ToString("0.###");

        if (mutationRateInput != null)
            mutationRateInput.text = evolutionManager.mutationRate.ToString("0.###");

        if (mutationStrengthInput != null)
            mutationStrengthInput.text = evolutionManager.mutationStrength.ToString("0.###");

        if (timeScaleInput != null)
            timeScaleInput.text = evolutionManager.timeScale.ToString("0.###");

        SyncFromSettingsToUI();
    }

    public void ApplyUIToManager()
    {
        bool changed = false;

        if (populationSizeInput != null && int.TryParse(populationSizeInput.text, out int pop))
        {
            EvolutionTunerSettings.populationSize = Mathf.Max(1, pop);
            changed = true;
        }

        if (generationDurationInput != null && float.TryParse(generationDurationInput.text, out float gd))
        {
            EvolutionTunerSettings.generationDuration = Mathf.Max(0.1f, gd);
            changed = true;
        }

        if (mutationRateInput != null && float.TryParse(mutationRateInput.text, out float mr))
        {
            EvolutionTunerSettings.mutationRate = Mathf.Clamp01(mr);
            changed = true;
        }

        if (mutationStrengthInput != null && float.TryParse(mutationStrengthInput.text, out float ms))
        {
            EvolutionTunerSettings.mutationStrength = Mathf.Max(0f, ms);
            changed = true;
        }

        if (timeScaleInput != null && float.TryParse(timeScaleInput.text, out float ts))
        {
            EvolutionTunerSettings.timeScale = Mathf.Clamp(ts, 0f, 50f);
            changed = true;

            if (evolutionManager != null)
                Time.timeScale = EvolutionTunerSettings.timeScale;
        }

        bool displayChanged = false;

        if (fullScreenModeDropdown != null)
        {
            int mode = fullScreenModeDropdown.value;
            mode = Mathf.Clamp(mode, 0, 3);
            if (EvolutionTunerSettings.fullScreenMode != mode)
            {
                EvolutionTunerSettings.fullScreenMode = mode;
                displayChanged = true;
            }
        }

        if (resolutionDropdown != null && resolutionDropdown.options != null && resolutionDropdown.options.Count > 0)
        {
            int idx = Mathf.Clamp(resolutionDropdown.value, 0, resolutionDropdown.options.Count - 1);
            string opt = resolutionDropdown.options[idx].text;

            if (!string.IsNullOrEmpty(opt))
            {
                string normalized = opt.Replace(" ", "");
                var parts = normalized.Split('x');
                if (parts.Length == 2 && int.TryParse(parts[0], out int parsedW2) && int.TryParse(parts[1], out int parsedH2))
                {
                    parsedW2 = Mathf.Max(320, parsedW2);
                    parsedH2 = Mathf.Max(200, parsedH2);

                    if (EvolutionTunerSettings.resolutionWidth != parsedW2)
                    {
                        EvolutionTunerSettings.resolutionWidth = parsedW2;
                        displayChanged = true;
                    }

                    if (EvolutionTunerSettings.resolutionHeight != parsedH2)
                    {
                        EvolutionTunerSettings.resolutionHeight = parsedH2;
                        displayChanged = true;
                    }
                }
            }
        }

        if (displayChanged)
        {
            Screen.fullScreenMode = (FullScreenMode)EvolutionTunerSettings.fullScreenMode;
            Screen.SetResolution(EvolutionTunerSettings.resolutionWidth, EvolutionTunerSettings.resolutionHeight, false);
        }

        if (evolutionManager != null && changed)
            EvolutionTunerSettings.ApplyTo(evolutionManager);
    }
}



