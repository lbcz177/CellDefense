using UnityEngine;

public class TowerController : MonoBehaviour
{
    public TowerDefinition Definition{ get; private set; }
    [SerializeField]
    private LayerMask enemyLayerMask;
    private float attackCooldown = 0f;
    private EnemyController currentTarget;

    public void Initialize(TowerDefinition definition)
    {
        if(definition == null)
        {
            throw new System.ArgumentNullException(nameof(definition));
        }
        if(definition.AttackRange <= 0f)
        {
            throw new System.ArgumentOutOfRangeException(nameof(definition.AttackRange), "Attack range must be greater than zero.");
        }
        if(definition.Damage <= 0f)
        {
            throw new System.ArgumentOutOfRangeException(nameof(definition.Damage), "Damage must be greater than zero.");
        }
        if(definition.AttackInterval <= 0f)
        {
            throw new System.ArgumentOutOfRangeException(nameof(definition.AttackInterval), "Attack interval must be greater than zero.");
        }
        if(enemyLayerMask.value == 0)
        {
            throw new System.ArgumentException("Enemy layer mask must be set.", nameof(enemyLayerMask));
        }
        Definition = definition;
        attackCooldown = 0f;
        currentTarget = null;
    }

    private void Update()
    {
        if (Definition == null)
        {
            return;
        }
        if(Time.deltaTime <= 0f)
        {
            return;
        }

        if (currentTarget != null && !IsCurrentTargetValid())
        {
            currentTarget = null;
        }

        attackCooldown -= Time.deltaTime;
        if (attackCooldown > 0f)
        {
            return;
        }

        if (currentTarget == null)
        {
            currentTarget = FindTarget();
        }
        if(currentTarget == null)
        {
            return;
        }

        Attack(currentTarget);
        attackCooldown = Definition.AttackInterval;
    }

    private bool IsCurrentTargetValid()
    {
        if (currentTarget == null)
        {
            return false;
        }
        if (!currentTarget.isActiveAndEnabled)
        {
            return false;
        }
        if (!currentTarget.CanBeTargeted)
        {
            return false;
        }
        if ((currentTarget.transform.position - transform.position).sqrMagnitude <= Definition.AttackRange * Definition.AttackRange)
        {
            return true;
        }
        return false;
    }

    private EnemyController FindTarget()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, Definition.AttackRange, enemyLayerMask);
        EnemyController bestTarget = null;
        float bestDistanceSquared = float.PositiveInfinity;
        foreach (var collider in colliders)
        {
            EnemyController candidate = collider.GetComponentInParent<EnemyController>();
            if(candidate == null)
            {
                continue;
            }
            else
            {
                if (candidate != null && candidate.CanBeTargeted)
                {
                    float distanceToCurrent = (candidate.transform.position - transform.position).sqrMagnitude;
                    if (distanceToCurrent < bestDistanceSquared)
                    {
                        bestDistanceSquared = distanceToCurrent;
                        bestTarget = candidate;
                    }
                }
            }
        }
        return bestTarget;
    }

    private void Attack(EnemyController target)
    {
        target.TakeDamage(Definition.Damage);
    }
}
