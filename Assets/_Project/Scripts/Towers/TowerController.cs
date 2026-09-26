using UnityEngine;
using Unity.Profiling;

public class TowerController : MonoBehaviour
{
    private static readonly ProfilerMarker SpawnProjectileMarker =
        new ProfilerMarker("CellDefense.Projectile.Spawn");

    public TowerDefinition Definition{ get; private set; }
    [SerializeField]
    private LayerMask enemyLayerMask;
    private float attackCooldown = 0f;
    private EnemyController currentTarget;
    private ProjectilePool projectilePool;
    private TowerAttackBehaviour attackBehaviour;

    public void Initialize(TowerDefinition definition, ProjectilePool pool)
    {
        TowerAttackBehaviour[] attackBehaviours = GetComponents<TowerAttackBehaviour>();
        if (attackBehaviours.Length > 1)
        {
            throw new System.InvalidOperationException("A tower can have only one attack behaviour.");
        }
        attackBehaviour = attackBehaviours.Length == 1 ? attackBehaviours[0] : null;

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
        if((attackBehaviour == null || attackBehaviour is AntibodyAttackBehaviour) &&
            definition.ProjectilePrefab == null)
        {
            throw new System.ArgumentException("Projectile prefab must be set.", nameof(definition.ProjectilePrefab));
        }
        if(enemyLayerMask.value == 0)
        {
            throw new System.ArgumentException("Enemy layer mask must be set.", nameof(enemyLayerMask));
        }
        if (pool == null)
        {
            throw new System.ArgumentNullException(nameof(pool));
        }
        Definition = definition;
        projectilePool = pool;
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
        if (attackBehaviour != null && !attackBehaviour.CanAttack)
        {
            return;
        }

        // Specialized towers reevaluate priority after each cooldown so newly marked
        // enemies and unmarked antibody targets can take precedence.
        if (attackBehaviour != null || currentTarget == null)
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
            if(candidate == null || !candidate.CanBeTargeted)
            {
                continue;
            }

            float candidateDistanceSquared = (candidate.transform.position - transform.position).sqrMagnitude;
            if (bestTarget == null || IsCandidateBetter(
                    candidate,
                    candidateDistanceSquared,
                    bestTarget,
                    bestDistanceSquared))
            {
                bestDistanceSquared = candidateDistanceSquared;
                bestTarget = candidate;
            }
        }
        return bestTarget;
    }

    private bool IsCandidateBetter(
        EnemyController candidate,
        float candidateDistanceSquared,
        EnemyController currentBest,
        float currentBestDistanceSquared)
    {
        if (attackBehaviour != null)
        {
            int candidatePriority = attackBehaviour.GetTargetPriority(candidate);
            int currentPriority = attackBehaviour.GetTargetPriority(currentBest);
            if (candidatePriority != currentPriority)
            {
                return candidatePriority > currentPriority;
            }
        }

        switch (Definition.TargetingMode)
        {
            case TargetingMode.NearestToTower:
                return candidateDistanceSquared < currentBestDistanceSquared;

            case TargetingMode.FarthestAlongPath:
                float candidateProgress = GetPathProgress(candidate);
                float currentBestProgress = GetPathProgress(currentBest);
                if (candidateProgress > currentBestProgress)
                {
                    return true;
                }
                if (Mathf.Approximately(candidateProgress, currentBestProgress))
                {
                    return candidateDistanceSquared < currentBestDistanceSquared;
                }
                return false;

            default:
                throw new System.ArgumentOutOfRangeException(nameof(Definition.TargetingMode));
        }
    }

    private static float GetPathProgress(EnemyController enemy)
    {
        PathFollower pathFollower = enemy.GetComponent<PathFollower>();
        if (pathFollower == null)
        {
            throw new System.InvalidOperationException("Target enemy must have a PathFollower component.");
        }

        return pathFollower.Progress;
    }

    private void Attack(EnemyController target)
    {
        if (attackBehaviour != null)
        {
            attackBehaviour.Attack(target, Definition.Damage, projectilePool);
            return;
        }

        using (SpawnProjectileMarker.Auto())
        {
            Projectile projectile = projectilePool.Get(transform.position);
            projectile.Initialize(target, Definition.Damage);
        }
    }
}
