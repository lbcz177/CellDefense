using Unity.VisualScripting;
using UnityEngine;

public class WaveController : MonoBehaviour
{
    [SerializeField] private EnemyController enemyPrefab;
    [SerializeField] private WaypointPath path;
    private EnemyController activeEnemy;

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

    private void SpawnNextEnemy()
    {
        if(enemyPrefab == null || path == null)
        {
            throw new System.InvalidOperationException("Enemy prefab or path is not assigned.");
        }

        activeEnemy = Instantiate(enemyPrefab);
        activeEnemy.Exited += ReportEnemyExited;
        activeEnemy.Initialize(path);
    }

    public void ReportEnemyExited(EnemyController enemy, EnemyExitReason reason)
    {
        if(activeEnemy == enemy)
        {
            activeEnemy.Exited -= ReportEnemyExited;
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
