using UnityEngine;
using TMPro;

public class EvolutionTimerUIController : MonoBehaviour
{
    [Header("UI (TMP)")]
    public TMP_Text generationText;
    public TMP_Text timeText;

    [Header("Mode")]
    [Tooltip("Menu/preview mode doesn’t require EvolutionManager. Gameplay mode can optionally sync from EvolutionManager.")]
    public bool gameplayMode = false;

    [Header("Sync (optional)" )]
    public EvolutionManager evolutionManager;

    [Tooltip("If true, use Time.unscaledDeltaTime so ESC/pause (Time.timeScale = 0) won’t stop the displayed timer.")]
    public bool useUnscaledTime = true;

    private float playmodeTotalTime;
    private int generation;

    void Awake()
    {
        generation = 0;
        playmodeTotalTime = 0f;

        ApplyText();
    }

    void Start()
    {
        if (gameplayMode && evolutionManager == null)
            evolutionManager = FindFirstObjectByType<EvolutionManager>();

        if (evolutionManager != null && gameplayMode)
        {
        }
    }

    void Update()
    {
        playmodeTotalTime += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        if (timeText != null)
            timeText.text = $"Time: {playmodeTotalTime:F1}s";

        if (generationText != null)
            generationText.text = $"Gen: {generation}";

        if (EvolutionTunerSettings.generationDuration > 0f)
        {
            int expectedGen = Mathf.FloorToInt(playmodeTotalTime / EvolutionTunerSettings.generationDuration);
            if (expectedGen != generation)
            {
                generation = expectedGen;

                if (generationText != null)
                    generationText.text = $"Gen: {generation}";
            }
        }
    }

    private void ApplyText()
    {
        if (timeText != null)
            timeText.text = $"Time: {playmodeTotalTime:F1}s";

        if (generationText != null)
            generationText.text = $"Gen: {generation}";
    }

    private static T FindFirstObjectByType<T>() where T : UnityEngine.Object
    {
#if UNITY_2023_1_OR_NEWER
        return UnityEngine.Object.FindFirstObjectByType<T>();
#else
        return UnityEngine.Object.FindObjectOfType<T>();
#endif
    }
}


