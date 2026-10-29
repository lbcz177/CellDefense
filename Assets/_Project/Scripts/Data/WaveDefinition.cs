using UnityEngine;
using System;

[Serializable]
public sealed class WaveEnemyGroup
{
    public EnemyDefinition enemy;
    [Min(1)] public int count = 6;
    [Min(1)] public int burstSize = 6;
    [Min(0f)] public float afterDelay = 1f;
    // Missing fields in legacy assets must keep round-robin spawning, even when an
    // importer initializes a newly added integer to zero rather than its initializer.
    public bool useExplicitEntrance;
    public int entranceIndex = -1;
}

[CreateAssetMenu(fileName = "WaveDefinition", menuName = "Cell Defense/Wave Definition")]
public class WaveDefinition : ScriptableObject
{
    [SerializeField] private EnemyDefinition enemy;
    [SerializeField, Min(1)] private int enemyCount = 5;
    [SerializeField, Min(0f)] private float spawnInterval = 1f;
    // OpenFieldPrototypeController groups spawns; legacy WaveController remains sequential.
    [SerializeField, Min(1)] private int burstSize = 1;
    [SerializeField, Min(0f)] private float burstInterval = 1f;
    [SerializeField] private WaveEnemyGroup[] groups;
    public bool HasMixedGroups => groups != null && groups.Length > 0;
    public int GroupCount => HasMixedGroups ? groups.Length : 1;
    public EnemyDefinition GetGroupEnemy(int index) => HasMixedGroups ? groups[index].enemy : enemy;
    public int GetGroupCount(int index) => HasMixedGroups ? groups[index].count : enemyCount;
    public int GetGroupBurstSize(int index) => HasMixedGroups ? groups[index].burstSize : BurstSize;
    public float GetGroupAfterDelay(int index) => HasMixedGroups ? groups[index].afterDelay : burstInterval;
    public int GetGroupEntrance(int index) => HasMixedGroups && groups[index].useExplicitEntrance ? groups[index].entranceIndex : -1;
    public int ScheduledEnemyCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < GroupCount; i++) count = checked(count + GetGroupCount(i));
            return count;
        }
    }

    public void Validate()
    {
        if (float.IsNaN(spawnInterval) || float.IsInfinity(spawnInterval) || spawnInterval < 0f ||
            float.IsNaN(burstInterval) || float.IsInfinity(burstInterval) || burstInterval < 0f)
            throw new ArgumentException("Wave intervals must be finite and non-negative.");
        for (int i = 0; i < GroupCount; i++)
        {
            if (HasMixedGroups && groups[i] == null) throw new ArgumentException("Wave group is missing.");
            EnemyDefinition definition = GetGroupEnemy(i);
            float delay = GetGroupAfterDelay(i);
            if (definition == null || definition.EnemyPrefab == null || GetGroupCount(i) < 1 ||
                GetGroupBurstSize(i) < 1 || GetGroupEntrance(i) < -1 || float.IsNaN(delay) || float.IsInfinity(delay) || delay < 0f)
                throw new ArgumentException("Invalid wave enemy, count, burst size or delay.");
            definition.Validate();
        }
    }
    public EnemyDefinition Enemy => enemy;
    public int EnemyCount => enemyCount;
    public float SpawnInterval => spawnInterval;
    // Older wave assets do not contain burstSize. Treat their default zero as one.
    public int BurstSize => Mathf.Max(1, burstSize);
    public float BurstInterval => burstInterval;

}
