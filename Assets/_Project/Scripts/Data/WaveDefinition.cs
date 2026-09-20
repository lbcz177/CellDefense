using UnityEngine;

[CreateAssetMenu(fileName = "WaveDefinition", menuName = "Cell Defense/Wave Definition")]
public class WaveDefinition : ScriptableObject
{
    [SerializeField] private EnemyDefinition enemy;
    [SerializeField, Min(1)] private int enemyCount = 5;
    [SerializeField, Min(0f)] private float spawnInterval = 1f;
    public EnemyDefinition Enemy => enemy;
    public int EnemyCount => enemyCount;
    public float SpawnInterval => spawnInterval;

}
