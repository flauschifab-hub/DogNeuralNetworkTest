using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayButton : MonoBehaviour
{
    public void switchScenes(string sceneName)
    {
        var tuner = UnityEngine.Object.FindFirstObjectByType<EvolutionTunerUI>();
        if (tuner != null)
            tuner.ApplyUIToManager();

        SceneManager.LoadScene(sceneName);
    }
}


