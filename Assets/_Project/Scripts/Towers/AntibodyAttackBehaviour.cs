using System;
using System.Collections.Generic;
using UnityEngine;

public class AntibodyAttackBehaviour : TowerAttackBehaviour
{
    [SerializeField, Min(0.01f)] private float markDuration = 4f;
    [SerializeField] private TimedSpriteIndicator memoryIndicator;
    private readonly Dictionary<AntigenId, float> knownAntigens = new Dictionary<AntigenId, float>();
    private readonly List<AntigenId> expiredAntigens = new List<AntigenId>();

    public override bool RequiresProjectile => true;
    public override bool RequiresDamage => false;
    public override bool CanAttack => isActiveAndEnabled && KnownAntigenCount > 0;
    public int KnownAntigenCount
    {
        get
        {
            RemoveExpiredAntigens();
            return knownAntigens.Count;
        }
    }

    public override void Initialize(TowerDefinition definition, LayerMask enemyLayers)
    {
        EnemyRangeQuery.RequirePositive(markDuration, nameof(markDuration));
        knownAntigens.Clear();
        expiredAntigens.Clear();
        if (memoryIndicator != null) memoryIndicator.Hide();
    }

    public void RememberAntigen(AntigenId antigenId, float duration)
    {
        EnemyRangeQuery.RequirePositive(duration, nameof(duration));
        if (!Enum.IsDefined(typeof(AntigenId), antigenId)) throw new ArgumentOutOfRangeException(nameof(antigenId));
        float until = Time.time + duration;
        if (knownAntigens.TryGetValue(antigenId, out float previousUntil)) until = Mathf.Max(until, previousUntil);
        knownAntigens[antigenId] = until;
        if (memoryIndicator != null)
        {
            memoryIndicator.ShowFor(duration);
        }
    }

    public bool KnowsAntigen(AntigenId antigenId)
    {
        return knownAntigens.TryGetValue(antigenId, out float knownUntil) &&
            Time.time < knownUntil;
    }

    private void OnDisable()
    {
        knownAntigens.Clear();
        expiredAntigens.Clear();
        if (memoryIndicator != null) memoryIndicator.Hide();
    }

    private void RemoveExpiredAntigens()
    {
        expiredAntigens.Clear();
        foreach (KeyValuePair<AntigenId, float> item in knownAntigens)
            if (Time.time >= item.Value) expiredAntigens.Add(item.Key);
        foreach (AntigenId antigen in expiredAntigens) knownAntigens.Remove(antigen);
    }

    public override bool CanTarget(EnemyController target)
    {
        if (target == null || !target.isActiveAndEnabled || !target.CanBeTargeted ||
            target.Definition == null || !KnowsAntigen(target.CurrentAntigenId)) return false;
        EnemyImmuneState state = target.GetComponent<EnemyImmuneState>();
        return state != null && state.isActiveAndEnabled;
    }

    public override int GetTargetPriority(EnemyController target)
    {
        if (target == null || target.Definition == null ||
            !KnowsAntigen(target.CurrentAntigenId))
        {
            return 0;
        }

        EnemyImmuneState state = target.GetComponent<EnemyImmuneState>();
        return state != null && !state.IsMarked ? 1 : 0;
    }

    public override void Attack(EnemyController target, float damage, ProjectilePool projectilePool)
    {
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
        if (projectilePool == null)
        {
            throw new ArgumentNullException(nameof(projectilePool));
        }
        if (markDuration <= 0f)
        {
            throw new InvalidOperationException("Antibody mark duration must be greater than zero.");
        }

        if (!CanTarget(target))
        {
            return;
        }
        EnemyImmuneState immuneState = target.GetComponent<EnemyImmuneState>();
        if (immuneState == null || !immuneState.isActiveAndEnabled)
        {
            throw new InvalidOperationException("Enemy prefab needs an enabled EnemyImmuneState component for antibody attacks.");
        }

        Projectile projectile = projectilePool.Get(transform.position);
        projectile.InitializeMark(target, markDuration);
    }
}
