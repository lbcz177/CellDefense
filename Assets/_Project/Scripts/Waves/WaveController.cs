using System;
using UnityEngine;

public class WaveController : MonoBehaviour
{
    [SerializeField] private WaypointPath path;
    private EnemyController activeEnemy;
    private EconomyService economyService;
    [SerializeField] private EnemyDefinition enemyDefinition;

    void Start()
    {
        StartWave();
    }
    public void StartWave()
    {
        if(activeEnemy != null)
        {
            return;
        }
        SpawnNextEnemy();
    }

    public void Initialize(EconomyService economyService)
    {
        if(economyService == null)
        {
            throw new ArgumentNullException(nameof(economyService));
        }

        this.economyService = economyService;
    }

    private void SpawnNextEnemy()
    {
        if(enemyDefinition == null || enemyDefinition.EnemyPrefab == null || path == null)
        {
            throw new InvalidOperationException("Enemy definition, prefab, or path is not assigned.");
        }

        activeEnemy = Instantiate(enemyDefinition.EnemyPrefab);
        activeEnemy.Exited += ReportEnemyExited;
        activeEnemy.Initialize(path, enemyDefinition);
    }

    public void ReportEnemyExited(EnemyController enemy, EnemyExitReason reason)
    {
        if(activeEnemy == enemy)
        {
            activeEnemy.Exited -= ReportEnemyExited;
            if(reason == EnemyExitReason.Leaked)
            {
                Debug.Log("Enemy reached the end of the path.");
            }
            else if(reason == EnemyExitReason.Killed)
            {
                Debug.Log("Enemy was killed.");
                int reward = enemy.Definition.KillReward;
                if(reward > 0)
                {
                    economyService.AddATP(reward);
                }
            }
            Destroy(enemy.gameObject);
            activeEnemy = null;
            EvaluateWaveComplete();
        }
    }

    private void EvaluateWaveComplete()
    {
        Debug.Log("Wave complete!");
    }
}
