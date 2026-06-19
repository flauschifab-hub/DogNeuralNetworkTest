using System;
using System.IO;
using UnityEngine;

public class EvolutionGenerationIO : MonoBehaviour
{
    [Header("References")]
    public EvolutionManager evolutionManager;

    [Header("Save/Load")]
    [Tooltip("Filename used inside Application.persistentDataPath.")]
    public string saveFileName = "generation_best.bin";

    private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

    private void Awake()
    {
        if (evolutionManager == null)
            evolutionManager = FindFirstObjectByType<EvolutionManager>();
    }

    public void FocusOnBest()
    {
        if (evolutionManager == null)
        {
            Debug.Log("[EvolutionGenerationIO] EvolutionManager not found.");
            return;
        }

        evolutionManager.SetFocusBestOnly(true);
    }

    public void SetFocusBestOnly(bool value)
    {
        if (evolutionManager == null)
            evolutionManager = FindFirstObjectByType<EvolutionManager>();

        if (evolutionManager == null)
        {
            Debug.Log("[EvolutionGenerationIO] EvolutionManager not found.");
            return;
        }

        evolutionManager.SetFocusBestOnly(value);
    }

    public void SaveGeneration()
    {
        if (evolutionManager == null)
            evolutionManager = FindFirstObjectByType<EvolutionManager>();

        if (evolutionManager == null)
        {
            Debug.Log("[EvolutionGenerationIO] EvolutionManager not found.");
            return;
        }

        NeuralNetwork snapshot = evolutionManager.GetBestSnapshotForUI();
        if (snapshot == null)
        {
            Debug.Log("[EvolutionGenerationIO] No best snapshot available yet.");
            return;
        }

        byte[] data = snapshot.ToBytes();
        File.WriteAllBytes(SavePath, data);
        Debug.Log($"[EvolutionGenerationIO] Saved generation snapshot to: {SavePath} ({data.Length} bytes)");
    }

    public void LoadGeneration()
    {
        if (evolutionManager == null)
            evolutionManager = FindFirstObjectByType<EvolutionManager>();

        if (evolutionManager == null)
        {
            Debug.Log("[EvolutionGenerationIO] EvolutionManager not found.");
            return;
        }

        if (!File.Exists(SavePath))
        {
            Debug.Log($"[EvolutionGenerationIO] Save file not found: {SavePath}");
            return;
        }

        byte[] data = File.ReadAllBytes(SavePath);
        NeuralNetwork loaded = NeuralNetwork.FromBytes(data);

        if (loaded == null)
        {
            Debug.Log("[EvolutionGenerationIO] Failed to deserialize neural network.");
            return;
        }

        evolutionManager.ApplyBestSnapshotFromUI(loaded);
        Debug.Log($"[EvolutionGenerationIO] Loaded generation snapshot from: {SavePath}");
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



