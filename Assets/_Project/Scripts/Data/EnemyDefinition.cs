using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Cell Defense/Enemy Definition")]
public class EnemyDefinition : ScriptableObject
{
    [SerializeField]
    private EnemyController enemyPrefab;
    [SerializeField, Min(0.01f)]
    private float maxHealth = 100f;
    [SerializeField, Min(0)]
    private int killReward = 10;
    public EnemyController EnemyPrefab => enemyPrefab;
    public float MaxHealth => maxHealth;
    public int KillReward => killReward;
}
