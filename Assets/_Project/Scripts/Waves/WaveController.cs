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
        activeEnemy.ReachedEnd += ReportEnemyExited;
        activeEnemy.Initialize(path);
    }

    public void ReportEnemyExited(EnemyController enemy)
    {
        if(activeEnemy == enemy)
        {
            activeEnemy.ReachedEnd -= ReportEnemyExited;
            Destroy(enemy);
            activeEnemy = null;
            EvaluateWaveComplete();
        }
    }

    private void EvaluateWaveComplete()
    {
        Debug.Log("Wave complete!");
    }
}
