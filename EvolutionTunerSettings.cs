using UnityEngine;

public static class EvolutionTunerSettings
{
    public static int populationSize = 20;
    public static float generationDuration = 20f;
    public static float mutationRate = 0.1f;
    public static float mutationStrength = 0.3f;
    public static float timeScale = 1f;

    public static int fullScreenMode = (int)FullScreenMode.Windowed;
    public static int resolutionWidth = 1280;
    public static int resolutionHeight = 720;

    public static void ResetToDefaults()
    {
        populationSize = 20;
        generationDuration = 20f;
        mutationRate = 0.1f;
        mutationStrength = 0.3f;
        timeScale = 1f;

        fullScreenMode = (int)FullScreenMode.Windowed;
        resolutionWidth = 1280;
        resolutionHeight = 720;
    }

    public static void ApplyTo(EvolutionManager manager)
    {
        if (manager == null) return;

        manager.populationSize = populationSize;
        manager.generationDuration = generationDuration;
        manager.mutationRate = mutationRate;
        manager.mutationStrength = mutationStrength;
        manager.timeScale = timeScale;
    }
}



