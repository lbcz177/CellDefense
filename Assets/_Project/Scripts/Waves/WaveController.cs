using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveController : MonoBehaviour
{
    public event Action WaveCompleted;

    [SerializeField] private WaypointPath path;
    [SerializeField] private WaveDefinition waveDefinition;
    private readonly List<EnemyController> activeEnemies = new List<EnemyController>();
    private EconomyService economyService;
    private LifeService lifeService;
    private bool allEnemiesSpawned;
    private bool waveRunning;

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

    public void StartWave()
    {
        if (waveRunning)
        {
            Debug.LogWarning("Wave is already running.");
            return;
        }
        if (activeEnemies.Count > 0)
        {
            return;
        }
        if (waveDefinition == null || waveDefinition.Enemy == null || waveDefinition.Enemy.EnemyPrefab == null || path == null)
        {
            throw new InvalidOperationException("Wave definition, enemy prefab, or path is not assigned.");
        }

        waveRunning = true;
        allEnemiesSpawned = false;
        StartCoroutine(SpawnEnemies());
    }

    private IEnumerator SpawnEnemies()
    {
        for (int i = 0; i < waveDefinition.EnemyCount; i++)
        {
            SpawnNextEnemy();
            if (i < waveDefinition.EnemyCount - 1)
            {
                yield return new WaitForSeconds(waveDefinition.SpawnInterval);
            }
        }

        allEnemiesSpawned = true;
        EvaluateWaveComplete();
    }

    private void SpawnNextEnemy()
    {
        EnemyDefinition definition = waveDefinition.Enemy;
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
        if (waveRunning && allEnemiesSpawned && activeEnemies.Count == 0)
        {
            waveRunning = false;
            Debug.Log("Wave complete!");
            WaveCompleted?.Invoke();
        }
    }
}