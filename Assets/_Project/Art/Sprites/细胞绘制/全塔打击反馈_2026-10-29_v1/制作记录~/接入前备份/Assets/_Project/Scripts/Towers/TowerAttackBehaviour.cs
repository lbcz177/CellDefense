using UnityEngine;

public abstract class TowerAttackBehaviour : MonoBehaviour
{
    public virtual bool RequiresProjectile => false;
    public virtual bool RequiresDamage => true;

    // Called by TowerController after its configuration and dependencies are assigned.
    public virtual void Initialize(TowerDefinition definition, LayerMask enemyLayers)
    {
    }

    // Additional readiness gate. TowerController still owns the regular attack cooldown.
    public virtual bool CanAttack => true;

    // Filter ineligible units before applying the shared front-first ordering.
    public virtual bool CanTarget(EnemyController target) => true;

    // In front-first targeting, this only breaks ties in remaining route distance.
    public virtual int GetTargetPriority(EnemyController target)
    {
        return 0;
    }

    // Resolve one attack or launch its projectile. damage already includes tower upgrades.
    public abstract void Attack(EnemyController target, float damage, ProjectilePool projectilePool);
}
