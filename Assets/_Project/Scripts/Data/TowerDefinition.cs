using UnityEngine;

public enum TargetingMode
{
    NearestToTower = 0,
    FarthestAlongPath = 1 // Shortest remaining route to the goal, including multiple entrances.
}

[CreateAssetMenu(fileName = "TowerDefinition", menuName = "Cell Defense/Tower Definition")]
public class TowerDefinition : ScriptableObject
{
    [SerializeField]
    private string displayName = "Tower";
    public string DisplayName => displayName;
    [SerializeField]
    private TowerController prefab;
    public TowerController Prefab => prefab;
    [SerializeField]
    private Sprite buildIcon;
    public Sprite BuildIcon => buildIcon;
    [SerializeField]
    private BuildPlacementKind placementKind = BuildPlacementKind.TowerSite;
    public BuildPlacementKind PlacementKind => placementKind;
    [SerializeField]
    private Projectile projectilePrefab;
    public Projectile ProjectilePrefab => projectilePrefab;
    [SerializeField, Min(1)]
    private int buildCost = 25;
    public int BuildCost => buildCost;
    [SerializeField, Min(0.01f)]
    private float attackRange = 2.5f;
    [SerializeField, Min(0f)]
    [Tooltip("Damage towers require a positive value. Support behaviours ignore this value and allow zero.")]
    private float damage = 25f;
    [SerializeField, Min(0.01f)]
    private float attackInterval = 1f;
    [SerializeField]
    [Tooltip("FarthestAlongPath prioritizes the shortest remaining route to the goal across all entrances.")]
    private TargetingMode targetingMode = TargetingMode.FarthestAlongPath;
    public float AttackRange => attackRange;
    public float AttackInterval => attackInterval;
    public float Damage => damage;
    public TargetingMode TargetingMode => targetingMode;
    [Header("Open field defense")]
    [SerializeField] private bool blocksEnemyRoute;
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField, Min(1)] private int interceptCapacity = 1;
    public bool BlocksEnemyRoute => blocksEnemyRoute;
    public float MaxHealth => maxHealth;
    public int InterceptCapacity => interceptCapacity;
}
