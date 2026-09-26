using UnityEngine;

public abstract class TowerAttackBehaviour : MonoBehaviour
{
    public virtual bool CanAttack => true;

    public virtual int GetTargetPriority(EnemyController target)
    {
        return 0;
    }

    public abstract void Attack(EnemyController target, float damage, ProjectilePool projectilePool);
}
