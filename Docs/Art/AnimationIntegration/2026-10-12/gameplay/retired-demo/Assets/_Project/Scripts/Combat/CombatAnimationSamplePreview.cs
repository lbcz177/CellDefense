using UnityEngine;

// A small repeatable preview using the same prefabs and combat code as the trial level.
public sealed class CombatAnimationSamplePreview : MonoBehaviour
{
    [SerializeField] private EnemyController enemy;
    [SerializeField] private TowerController tower;
    [SerializeField] private EnemyDefinition enemyDefinition;
    [SerializeField] private TowerDefinition towerDefinition;
    [SerializeField] private ProjectilePool projectilePool;
    [SerializeField] private WaypointPath path;
    [SerializeField] private BuildSlot buildSlot;
    private Health enemyHealth;
    private float nextSpawnAt;
    private float lastHealth;

    public int CoreHits { get; private set; }
    public int ProjectileHits { get; private set; }
    public EnemyController Enemy => enemy;
    public TowerController Tower => tower;
    public ProjectilePool Pool => projectilePool;

    private void Start()
    {
        if (enemy == null || tower == null || enemyDefinition == null || towerDefinition == null ||
            projectilePool == null || path == null || buildSlot == null)
        {
            throw new System.InvalidOperationException("The animation preview is missing a scene reference.");
        }
        enemyHealth = enemy.GetComponent<Health>();
        enemy.Exited += OnEnemyExited;
        enemyHealth.HealthChanged += OnHealthChanged;
        projectilePool.Initialize();
        if (buildSlot.CanBuild()) buildSlot.Occupy(tower);
        tower.Initialize(towerDefinition, projectilePool, buildSlot);
        SpawnEnemy();
    }

    private void Update()
    {
        if (enemy != null && !enemy.gameObject.activeSelf && Time.time >= nextSpawnAt)
        {
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        enemy.gameObject.SetActive(true);
        enemy.Initialize(path, enemyDefinition);
    }

    private void OnHealthChanged(float current, float maximum)
    {
        if (current < lastHealth) ProjectileHits++;
        lastHealth = current;
    }

    private void OnEnemyExited(EnemyController exited, EnemyExitReason reason)
    {
        if (reason == EnemyExitReason.Leaked) CoreHits++;
        exited.gameObject.SetActive(false);
        nextSpawnAt = Time.time + 0.35f;
    }

    private void OnDestroy()
    {
        if (enemy != null) enemy.Exited -= OnEnemyExited;
        if (enemyHealth != null) enemyHealth.HealthChanged -= OnHealthChanged;
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(16,12,450,24), "Neutrophil vs Staphylococcus - animation sample");
        GUI.Label(new Rect(16,36,450,24), "Move / charge / release / hit flash / core attack");
        GUI.Label(new Rect(16,60,450,24), "Projectile hits: " + ProjectileHits + "    Core hits: " + CoreHits);
    }
}
