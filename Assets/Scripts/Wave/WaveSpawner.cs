using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    Transform spawnPoint;
    Transform[] waypoints;
    [SerializeField] GameObject[] enemyPrefabs;
    [SerializeField] Wave[] waves;
    int alliveEnemyCount;
    bool allWaveFinished;
    public static WaveSpawner Instance { get; private set; }

    [System.Serializable]
    public class Wave
    {
        public int enemyPrefabIndex;
        public int count;
        public float spawnInterval;
        public float delayBeforeWave;
    }

    void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }
    void Start()
    {
        GameObject path = GameObject.Find("Path");
        if(path == null)
        {
            return;
        }
        Transform pathParent = path.transform;
        int childCount = pathParent.childCount;
        waypoints = new Transform[childCount];
        for(int i = 0; i < childCount; i++)
        {
            waypoints[i] = pathParent.GetChild(i);
        }

        spawnPoint = waypoints[0];

        StartCoroutine(SpawnAllWaves());
        
    }

    IEnumerator SpawnAllWaves()//协程
    {
        if (waves == null || waves.Length == 0) yield break;
        for(int i = 0; i < waves.Length; i++)
        {
            Wave wave = waves[i];
            yield return new WaitForSeconds(wave.delayBeforeWave);
        
            for (int j = 0; j < wave.count; j++)
            {
                SpawnEnemy(wave.enemyPrefabIndex);
                yield return new WaitForSeconds(wave.spawnInterval);  // 等间隔
            }
        }
        allWaveFinished = true;
    }

    void SpawnEnemy(int i)
    {
        GameObject enemy = Instantiate(enemyPrefabs[i], spawnPoint.position, Quaternion.identity);
        enemy.GetComponent<EnemyFindWay>().SetWaypoints(waypoints);
        alliveEnemyCount++;
    }

    public void OnEnemyDeath()
    {
        alliveEnemyCount--;
        if(alliveEnemyCount == 0 && allWaveFinished)
        {
            //胜利
            StartCoroutine(DelayedVictory());
        }
    }

    IEnumerator DelayedVictory()
    {
    yield return new WaitForSeconds(0.5f);
    GameManager.Instance.Victory();
    }
}
