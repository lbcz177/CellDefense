using System;
using System.Collections.Generic;
using UnityEngine;

public class AntibodyAttackBehaviour : TowerAttackBehaviour
{
    [SerializeField, Min(0.01f)] private float markDuration = 4f;
    private readonly Dictionary<AntigenId, float> knownAntigens = new Dictionary<AntigenId, float>();

    public void RememberAntigen(AntigenId antigenId, float duration)
    {
        if (duration <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        knownAntigens[antigenId] = Time.time + duration;
    }

    private bool KnowsAntigen(AntigenId antigenId)
    {
        return knownAntigens.TryGetValue(antigenId, out float knownUntil) &&
            Time.time < knownUntil;
    }

    private void OnDisable()
    {
        knownAntigens.Clear();
    }

    public override int GetTargetPriority(EnemyController target)
    {
        if (target == null || target.Definition == null ||
            !KnowsAntigen(target.Definition.AntigenId))
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

        bool canMark = target.Definition != null &&
            KnowsAntigen(target.Definition.AntigenId);
        if (canMark && target.GetComponent<EnemyImmuneState>() == null)
        {
            throw new InvalidOperationException("Enemy prefab needs an EnemyImmuneState component for antibody attacks.");
        }

        Projectile projectile = projectilePool.Get(transform.position);
        projectile.Initialize(target, damage, canMark ? markDuration : 0f);
    }
}
