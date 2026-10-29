using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(TowerController))]
public class FibrinSlowAura : TowerAttackBehaviour
{
    [SerializeField, Range(0f, 0.95f)] private float slowFraction = 0.4f;
    [FormerlySerializedAs("scanlnterval")]
    [SerializeField, Min(0.01f)] private float scanInterval = 0.1f;
    [SerializeField, Min(0.01f)] private float refreshDuration = 0.25f;
    [SerializeField] private LayerMask enemyLayerMask;
    private TowerController tower;
    private ContactFilter2D targetFilter;
    private readonly List<Collider2D> candidates = new List<Collider2D>(64);
    private readonly HashSet<EnemyController> visited = new HashSet<EnemyController>();
    private float scanCooldown;
    private int sourceId;
    private bool isConfigured;
    private bool reportedMissingSlowState;

    public override bool CanAttack => false;
    public override bool RequiresDamage => false;

    private void Awake()
    {
        tower = GetComponent<TowerController>();
        sourceId = GetInstanceID();
    }

    public override void Initialize(TowerDefinition definition, LayerMask enemyLayers)
    {
        tower = GetComponent<TowerController>();
        sourceId = GetInstanceID();
        isConfigured = false;
        if (enemyLayerMask.value == 0) enemyLayerMask = enemyLayers;
        ValidateConfiguration();
        targetFilter = new ContactFilter2D();
        targetFilter.SetLayerMask(enemyLayerMask);
        targetFilter.useTriggers = true;
        isConfigured = true;
        scanCooldown = 0f;
        reportedMissingSlowState = false;
        candidates.Clear();
        visited.Clear();
    }

    private void OnEnable()
    {
        scanCooldown = 0f;
        reportedMissingSlowState = false;
    }

    private void OnDisable()
    {
        scanCooldown = 0f;
        candidates.Clear();
        visited.Clear();
        // Each enemy's source record expires independently after the last refresh.
    }

    private void ValidateConfiguration()
    {
        if (tower == null)
        {
            throw new InvalidOperationException("A slow aura requires a TowerController on the same object.");
        }
        if (enemyLayerMask.value == 0)
        {
            throw new InvalidOperationException("Assign the slow aura's enemy layer mask.");
        }
        if (float.IsNaN(slowFraction) || float.IsInfinity(slowFraction) ||
            slowFraction < 0f || slowFraction >= 1f)
        {
            throw new InvalidOperationException("Slow fraction must be finite and in [0, 1).");
        }
        if (float.IsNaN(scanInterval) || float.IsInfinity(scanInterval) || scanInterval <= 0f)
        {
            throw new InvalidOperationException("Slow aura scan interval must be finite and positive.");
        }
        if (float.IsNaN(refreshDuration) || float.IsInfinity(refreshDuration) ||
            refreshDuration <= scanInterval)
        {
            throw new InvalidOperationException("Slow refresh duration must exceed the scan interval.");
        }
    }

    private void RefreshAura()
    {
        candidates.Clear();
        visited.Clear();
        float range = tower.Definition.AttackRange;
        float rangeSquared = range * range;
        int count = Physics2D.OverlapCircle(transform.position, range, targetFilter, candidates);
        for (int i = 0; i < count; i++)
        {
            Collider2D candidate = candidates[i];
            if (candidate == null) continue;

            EnemyController enemy = candidate.GetComponentInParent<EnemyController>();
            if (enemy == null || !enemy.isActiveAndEnabled || !enemy.CanBeTargeted)
            {
                continue;
            }
            Vector2 offset = (Vector2)enemy.transform.position - (Vector2)transform.position;
            if (offset.sqrMagnitude > rangeSquared || !visited.Add(enemy))
            {
                continue;
            }

            EnemySlowState slowState = enemy.GetComponent<EnemySlowState>();
            if (slowState == null || !slowState.isActiveAndEnabled)
            {
                if (!reportedMissingSlowState)
                {
                    Debug.LogError("Slow aura targets need an enabled EnemySlowState on their enemy prefab.", enemy);
                    reportedMissingSlowState = true;
                }
                continue;
            }
            slowState.ApplySlow(slowFraction, sourceId, refreshDuration);
        }
    }

    private void Update()
    {
        if (!isConfigured || tower == null || !tower.isActiveAndEnabled ||
            tower.Definition == null || Time.deltaTime <= 0f)
        {
            return;
        }
        scanCooldown -= Time.deltaTime;
        if (scanCooldown > 0f) return;

        RefreshAura();
        scanCooldown = scanInterval;
    }

    public override void Attack(EnemyController target, float damage, ProjectilePool projectilePool)
    {
        // CanAttack disables ordinary attacks; this aura refreshes all targets in Update.
    }
}
