using System;
using System.Collections.Generic;
using UnityEngine;

internal readonly struct EnemyTargetSnapshot
{
    public readonly EnemyController Enemy;
    public readonly uint SpawnVersion;
    public readonly Vector2 Position;

    public EnemyTargetSnapshot(EnemyController enemy)
    {
        Enemy = enemy;
        SpawnVersion = enemy.SpawnVersion;
        Position = enemy.transform.position;
    }

    public bool IsValid => Enemy != null && Enemy.isActiveAndEnabled &&
        Enemy.CanBeTargeted && Enemy.SpawnVersion == SpawnVersion;
}

// Each strategy owns a query so nested attacks cannot overwrite another tower's results.
internal sealed class EnemyRangeQuery
{
    private readonly ContactFilter2D filter;
    private readonly List<Collider2D> colliders = new List<Collider2D>(64);
    private readonly HashSet<EnemyController> uniqueEnemies = new HashSet<EnemyController>();
    private readonly List<EnemyTargetSnapshot> targets = new List<EnemyTargetSnapshot>(32);

    public IReadOnlyList<EnemyTargetSnapshot> Targets => targets;

    public EnemyRangeQuery(LayerMask enemyLayers)
    {
        if (enemyLayers.value == 0)
            throw new ArgumentException("Enemy layers must be assigned.", nameof(enemyLayers));

        ContactFilter2D configuredFilter = new ContactFilter2D();
        configuredFilter.SetLayerMask(enemyLayers);
        configuredFilter.useTriggers = true;
        filter = configuredFilter;
    }

    public void Collect(Vector2 center, float radius)
    {
        Clear();
        float radiusSquared = radius * radius;
        int count = Physics2D.OverlapCircle(center, radius, filter, colliders);
        for (int i = 0; i < count; i++)
        {
            Collider2D collider = colliders[i];
            if (collider == null) continue;
            EnemyController enemy = collider.GetComponentInParent<EnemyController>();
            if (enemy == null || !enemy.isActiveAndEnabled || !enemy.CanBeTargeted) continue;
            Vector2 offset = (Vector2)enemy.transform.position - center;
            if (offset.sqrMagnitude > radiusSquared || !uniqueEnemies.Add(enemy)) continue;
            targets.Add(new EnemyTargetSnapshot(enemy));
        }
    }

    public void Clear()
    {
        colliders.Clear();
        uniqueEnemies.Clear();
        targets.Clear();
    }

    public static void RequirePositive(float value, string parameterName)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            throw new ArgumentOutOfRangeException(parameterName, "Value must be finite and positive.");
    }
}
