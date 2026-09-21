using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveController : MonoBehaviour
{
    public event Action<int, int> WaveChanged;
    public event Action AllWavesCompleted;

    [SerializeField] private WaypointPath path;
    [SerializeField] private WaveSequenceDefinition waveSequence;
    private readonly List<EnemyController> activeEnemies = new List<EnemyController>();
    private EconomyService economyService;
    private LifeService lifeService;
    private int currentWaveIndex = -1;
    private bool allEnemiesSpawned;
    private bool waveRunning;
    private bool sequenceRunning;
    private bool hasCompletedSequence;

    public int CurrentWaveNumber => currentWaveIndex >= 0 ? currentWaveIndex + 1 : 0;
    public int TotalWaveCount => waveSequence != null ? waveSequence.WaveCount : 0;

    public void Initialize(EconomyService economyService, LifeService lifeService)
    {
        if (economyService == null)
        {
            throw new ArgumentNullException(nameof(economyService));
        }
        if (lifeService == null)
        {
            throw new ArgumentNullException(nameof(lifeService));
        }

        this.economyService = economyService;
        this.lifeService = lifeService;
    }

    public void StartSequence()
    {
        if (sequenceRunning)
        {
            Debug.LogWarning("Wave sequence is already running.");
            return;
        }
        if (activeEnemies.Count > 0)
        {
            return;
        }
        if (waveSequence == null || waveSequence.WaveCount == 0 || path == null)
        {
            throw new InvalidOperationException("Wave sequence or path is not assigned.");
        }

        sequenceRunning = true;
        hasCompletedSequence = false;
        currentWaveIndex = 0;
        StartCurrentWave();
    }

    private void StartCurrentWave()
    {
        if (!sequenceRunning || waveRunning || activeEnemies.Count > 0)
        {
            return;
        }

        WaveDefinition wave = waveSequence.GetWave(currentWaveIndex);
        ValidateWave(wave);
        waveRunning = true;
        allEnemiesSpawned = false;
        WaveChanged?.Invoke(CurrentWaveNumber, TotalWaveCount);
        StartCoroutine(SpawnEnemies(wave));
    }

    private static void ValidateWave(WaveDefinition wave)
    {
        if (wave.Enemy == null || wave.Enemy.EnemyPrefab == null)
        {
            throw new InvalidOperationException("Wave enemy or enemy prefab is not assigned.");
        }
    }

    private IEnumerator SpawnEnemies(WaveDefinition wave)
    {
        for (int i = 0; i < wave.EnemyCount; i++)
        {
            SpawnNextEnemy(wave);
            if (i < wave.EnemyCount - 1)
            {
                yield return new WaitForSeconds(wave.SpawnInterval);
            }
        }

        allEnemiesSpawned = true;
        EvaluateWaveComplete();
    }

    private void SpawnNextEnemy(WaveDefinition wave)
    {
        EnemyDefinition definition = wave.Enemy;
        EnemyController enemy = Instantiate(definition.EnemyPrefab);
        enemy.Exited += ReportEnemyExited;
        activeEnemies.Add(enemy);
        enemy.Initialize(path, definition);
    }

    public void ReportEnemyExited(EnemyController enemy, EnemyExitReason reason)
    {
        if (!activeEnemies.Remove(enemy))
        {
            Debug.LogWarning("Enemy not found in active enemies list.");
            return;
        }

        enemy.Exited -= ReportEnemyExited;
        if (reason == EnemyExitReason.Leaked)
        {
            Debug.Log("Enemy reached the end of the path.");
            lifeService.TryLoseLife(1);
        }
        else if (reason == EnemyExitReason.Killed)
        {
            Debug.Log("Enemy was killed.");
            int reward = enemy.Definition.KillReward;
            if (reward > 0)
            {
                economyService.AddATP(reward);
            }
        }

        Destroy(enemy.gameObject);
        EvaluateWaveComplete();
    }

    private void EvaluateWaveComplete()
    {
        if (sequenceRunning == false || waveRunning == false)
        {
            return;
        }
        if (allEnemiesSpawned == false || activeEnemies.Count > 0)
        {
            return;
        }

        bool isLastWave = currentWaveIndex >= waveSequence.WaveCount - 1;
        waveRunning = false;
        if (isLastWave)
        {
            CompleteSequence();
            return;
        }

        StartCoroutine(StartNextWaveAfterDelay());
    }

    private IEnumerator StartNextWaveAfterDelay()
    {
        yield return new WaitForSeconds(waveSequence.InterWaveDelay);
        if (!sequenceRunning || hasCompletedSequence)
        {
            yield break;
        }

        currentWaveIndex++;
        StartCurrentWave();
    }

    private void CompleteSequence()
    {
        if (hasCompletedSequence)
        {
            return;
        }

        hasCompletedSequence = true;
        sequenceRunning = false;
        waveRunning = false;
        Debug.Log("All waves complete!");
        AllWavesCompleted?.Invoke();
    }
}
