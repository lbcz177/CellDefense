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
    [SerializeField]
    private AntigenId antigenId = AntigenId.PrototypeA;
    [SerializeField]
    private bool engulfImmune;
    [SerializeField] private string displayName = "病原体";
    [SerializeField, Min(0.01f)] private float baseMoveSpeed = 2f;
    [SerializeField, Min(1)] private int leakDamage = 1;
    [SerializeField] private bool isBoss;
    [SerializeField] private bool complementSensitive;
    public bool ComplementSensitive => complementSensitive;
    [SerializeField] private bool inflammationSource;
    public bool IsInflammationSource => inflammationSource;
    [SerializeField, Range(0f, 0.95f)] private float armorReduction;
    [SerializeField, Min(0.01f)] private float armorBreakDuration = 3f;
    [SerializeField, Min(0f)] private float initialShield;
    [SerializeField, Range(0f, 1f)] private float slowEffectiveness = 1f;
    [SerializeField, Range(0f, 1f)] private float stunEffectiveness = 1f;
    [SerializeField, Min(0.01f)] private float stunRecoveryDuration = 0.5f;
    [SerializeField, Min(0.01f)] private float contactDamage = 10f;
    [SerializeField, Min(0.01f)] private float contactAttackInterval = 1f;
    public float ContactDamage => contactDamage;
    public float ContactAttackInterval => contactAttackInterval;
    public string DisplayName => displayName;
    public float BaseMoveSpeed => baseMoveSpeed;
    public int LeakDamage => leakDamage;
    public bool IsBoss => isBoss;
    public float ArmorReduction => armorReduction;
    public float ArmorBreakDuration => armorBreakDuration;
    public float InitialShield => initialShield;
    public float SlowEffectiveness => slowEffectiveness;
    public float StunEffectiveness => stunEffectiveness;
    public float StunRecoveryDuration => stunRecoveryDuration;

    public void Validate()
    {
        EnemyRangeQuery.RequirePositive(maxHealth, nameof(maxHealth));
        EnemyRangeQuery.RequirePositive(baseMoveSpeed, nameof(baseMoveSpeed));
        EnemyRangeQuery.RequirePositive(armorBreakDuration, nameof(armorBreakDuration));
        EnemyRangeQuery.RequirePositive(stunRecoveryDuration, nameof(stunRecoveryDuration));
        EnemyRangeQuery.RequirePositive(contactDamage, nameof(contactDamage));
        EnemyRangeQuery.RequirePositive(contactAttackInterval, nameof(contactAttackInterval));
        if (killReward < 0 || leakDamage < 1 || !System.Enum.IsDefined(typeof(AntigenId), antigenId))
            throw new System.ArgumentException("Invalid enemy reward, leak damage or antigen.");
        if (!FiniteRange(armorReduction, 0.95f) || !FiniteRange(slowEffectiveness, 1f) ||
            !FiniteRange(stunEffectiveness, 1f) || float.IsNaN(initialShield) ||
            float.IsInfinity(initialShield) || initialShield < 0f)
            throw new System.ArgumentException("Invalid enemy defense or control configuration.");
    }

    private static bool FiniteRange(float value, float maximum) =>
        !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f && value <= maximum;
    public EnemyController EnemyPrefab => enemyPrefab;
    public float MaxHealth => maxHealth;
    public int KillReward => killReward;
    public AntigenId AntigenId => antigenId;
    public bool CanBeEngulfed => !engulfImmune;
}
