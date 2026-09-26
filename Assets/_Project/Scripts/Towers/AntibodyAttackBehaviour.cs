using System;
using UnityEngine;

public class AntibodyAttackBehaviour : TowerAttackBehaviour
{
    [SerializeField] private AntigenId prototypeKnownAntigen = AntigenId.PrototypeA;
    [SerializeField, Min(0.01f)] private float markDuration = 4f;

    public override int GetTargetPriority(EnemyController target)
    {
        if (target == null || target.Definition == null ||
            target.Definition.AntigenId != prototypeKnownAntigen)
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
            target.Definition.AntigenId == prototypeKnownAntigen;
        if (canMark && target.GetComponent<EnemyImmuneState>() == null)
        {
            throw new InvalidOperationException("Enemy prefab needs an EnemyImmuneState component for antibody attacks.");
        }

        Projectile projectile = projectilePool.Get(transform.position);
        projectile.Initialize(target, damage, canMark ? markDuration : 0f);
    }
}
