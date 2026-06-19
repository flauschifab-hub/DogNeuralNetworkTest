using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class EvolutionManager : MonoBehaviour
{
    [Header("Prefab & Spawn")]
    public GameObject dogPrefab;
    public Transform spawnPoint;

    [Header("Evolution")]
    public int populationSize = 20;
    public float generationDuration = 8f;
    public float mutationRate = 0.1f;
    public float mutationStrength = 0.3f;
    public int eliteCount = 1;

    [Header("Speed")]
    [Range(1f, 20f)]
    public float timeScale = 1f;

    private List<DogAgent> population = new List<DogAgent>();
    private List<NeuralNetwork> networks = new List<NeuralNetwork>();
    private int generation = 0;

    private NeuralNetwork bestEverNetwork;
    private float bestEverFitness = float.NegativeInfinity;

    public TMP_Text generationText;
    public TMP_Text timeText;

    private float playmodeTotalTime = 0f;
    private float generationStartTime = 0f;

    private const int INPUTS = 28;
    private const int HIDDEN = 16;
    private const int OUTPUTS = 8;

    void Awake()
    {
        EvolutionTunerSettings.ApplyTo(this);
        ApplyTimeScale();
    }

    void Start()
    {
        InitFirstGeneration();
        playmodeTotalTime = 0f;
        generationStartTime = Time.time;
        StartCoroutine(RunGenerations());
    }

    void ApplyTimeScale()
    {
        Time.timeScale = timeScale;
    }

    void Update()
    {
        playmodeTotalTime += UnityEngine.Time.deltaTime;

        if (timeText != null)
            timeText.text = $"Time: {playmodeTotalTime:F1}s";

        if (generationText != null)
            generationText.text = $"Gen: {generation}";
    }

    void InitFirstGeneration()
    {
        bestEverNetwork = null;
        bestEverFitness = float.NegativeInfinity;

        networks.Clear();
        for (int i = 0; i < populationSize; i++)
            networks.Add(new NeuralNetwork(INPUTS, HIDDEN, OUTPUTS));
    }

    IEnumerator RunGenerations()
    {
        while (true)
        {
            generation++;
            Debug.Log($"=== Generation {generation} ===");
            generationStartTime = Time.time;

            focusedBestDog = null;
            focusedBestX = float.NegativeInfinity;

            SpawnPopulation();

            if (focusBestOnly)
                UpdateFocusedBestAndVisibility(forceCameraMove: false, forceVisibilityUpdate: true);

            yield return new WaitForSecondsRealtime(generationDuration);

            EvolveNextGeneration();
            DestroyPopulation();
        }
    }

    void SpawnPopulation()
    {
        population.Clear();
        for (int i = 0; i < populationSize; i++)
        {
            GameObject go = Instantiate(dogPrefab, spawnPoint.position, Quaternion.identity);

            SetDogVisible(go, true);

            DogAgent agent = go.GetComponent<DogAgent>();
            agent.Init(networks[i]);
            population.Add(agent);
        }
    }

    void EvolveNextGeneration()
    {
        var ranked = population
            .OrderByDescending(a => a.fitness)
            .ToList();

        Debug.Log($"Best: {ranked[0].fitness:F2}m  |  Worst: {ranked[^1].fitness:F2}m");

        int bestIdx = population.IndexOf(ranked[0]);
        NeuralNetwork genBestNetwork = networks[bestIdx];
        float genBestFitness = ranked[0].fitness;

        if (genBestFitness > bestEverFitness || bestEverNetwork == null)
        {
            bestEverFitness = genBestFitness;
            bestEverNetwork = genBestNetwork.Clone();
        }

        NeuralNetwork parent = bestEverNetwork;

        networks.Clear();

        for (int i = 0; i < populationSize; i++)
        {
            NeuralNetwork child = parent.Clone();

            if (i != 0)
                child.Mutate(mutationRate, mutationStrength);

            networks.Add(child);
        }
    }

    void DestroyPopulation()
    {
        foreach (var agent in population)
            if (agent != null) Destroy(agent.gameObject);
        population.Clear();
    }

    public NeuralNetwork GetBestSnapshotForUI()
    {
        if (bestEverNetwork == null) return null;
        return bestEverNetwork.Clone();
    }

    public void ApplyBestSnapshotFromUI(NeuralNetwork snapshot)
    {
        if (snapshot == null) return;

        bestEverNetwork = snapshot.Clone();

        if (bestEverFitness < 0f)
            bestEverFitness = 0f;

        networks.Clear();
        for (int i = 0; i < populationSize; i++)
            networks.Add(bestEverNetwork.Clone());

        Debug.Log($"[EvolutionManager] Applied loaded best snapshot. weightCount={bestEverNetwork.weights?.Length ?? 0}");
    }

    [Header("Focus UI")]
    [Tooltip("If true, hide all dogs except the current best (furthest along +X).")]
    public bool focusBestOnly = false;

    private DogAgent focusedBestDog;
    private float focusedBestX = float.NegativeInfinity;

    public void FocusCameraOnBest()
    {
        SetFocusBestOnly(true);
    }

    public void SetFocusBestOnly(bool value)
    {
        focusBestOnly = value;

        focusedBestDog = null;
        focusedBestX = float.NegativeInfinity;

        if (spawnPoint == null) return;

        UpdateFocusedBestAndVisibility(
            forceCameraMove: focusBestOnly,
            forceVisibilityUpdate: true);

        Debug.Log($"[EvolutionManager] focusBestOnly set to {focusBestOnly}.");
    }

    void UpdateFocusedBestAndVisibility(bool forceCameraMove, bool forceVisibilityUpdate)
    {
        if (population == null || population.Count == 0)
            return;

        bool cameraMoveRequested = forceCameraMove;

        if (!focusBestOnly)
        {
            for (int i = 0; i < population.Count; i++)
            {
                var a = population[i];
                if (a == null) continue;
                SetDogVisible(a.gameObject, true);
            }

            focusedBestDog = null;
            focusedBestX = float.NegativeInfinity;
            return;
        }

        DogAgent best = null;
        float bestX = float.NegativeInfinity;

        for (int i = 0; i < population.Count; i++)
        {
            var a = population[i];
            if (a == null) continue;

            float x = a.transform.position.x;
            if (x > bestX)
            {
                best = a;
                bestX = x;
            }
        }

        bool bestChanged = (best != focusedBestDog);

        focusedBestDog = best;
        focusedBestX = bestX;

        if (forceVisibilityUpdate || bestChanged)
        {
            for (int i = 0; i < population.Count; i++)
            {
                var a = population[i];
                if (a == null) continue;
                SetDogVisible(a.gameObject, a == focusedBestDog);
            }
        }

        if ((cameraMoveRequested || bestChanged) && focusBestOnly)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                if (focusedBestDog != null)
                {
                    var t = focusedBestDog.transform.position;
                    cam.transform.position = new Vector3(t.x, t.y, cam.transform.position.z);
                }
                else if (spawnPoint != null)
                {
                    cam.transform.position = new Vector3(spawnPoint.position.x, spawnPoint.position.y, cam.transform.position.z);
                }
            }
        }
    }

    void SetDogVisible(GameObject dogRoot, bool visible)
    {
        if (dogRoot == null) return;

        var renderers = dogRoot.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = visible;
    }
}


