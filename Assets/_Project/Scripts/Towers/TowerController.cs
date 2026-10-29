using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

public class TowerController : MonoBehaviour
{
    private static readonly ProfilerMarker SpawnProjectileMarker =
        new ProfilerMarker("CellDefense.Projectile.Spawn");
    private static readonly ProfilerMarker TargetingMarker =
        new ProfilerMarker("CellDefense.Targeting.Query");

    public TowerDefinition Definition{ get; private set; }
    public int BuildCostPaid { get; private set; }
    public int UpgradeCostPaid { get; private set; }
    public bool IsUpgraded { get; private set; }
    public const int UpgradeDamageBonusPercent = 50;
    public bool SupportsDamageUpgrade => Definition != null && Definition.Damage > 0f &&
        (attackBehaviour == null || attackBehaviour.RequiresDamage);
    public float CurrentDamage => Definition == null ||
        (attackBehaviour != null && !attackBehaviour.RequiresDamage) ? 0f :
        Definition.Damage * (IsUpgraded ? 1f + UpgradeDamageBonusPercent / 100f : 1f) * EnvironmentalDamageMultiplier;
    public float EnvironmentalDamageMultiplier { get; set; } = 1f;
    private const float UpgradeVisualScale = 1.15f;
    [SerializeField]
    private LayerMask enemyLayerMask;
    [SerializeField]
    private bool samplesAntigenOnHit;
    [SerializeField, Min(0.01f)]
    private float antigenMemoryDuration = 8f;
    private float attackCooldown = 0f;
    private EnemyController currentTarget;
    private uint currentTargetSpawnVersion;
    private readonly List<Collider2D> targetCandidates = new List<Collider2D>(64);
    private ContactFilter2D targetFilter;
    private ProjectilePool projectilePool;
    private TowerAttackBehaviour attackBehaviour;
    private BuildSlot occupiedSlot;
    private static readonly int AttackTrigger = Animator.StringToHash("Attack");
    private Animator animator;
    [SerializeField] private bool releaseOnAnimationEvent;
    [SerializeField] private Vector2 muzzleLocalOffset;
    [SerializeField] private bool aimVisualAtTarget;
    private SpriteRenderer bodyRenderer;
    [SerializeField] private Sprite projectileVisual;
    [SerializeField, Min(0.01f)] private float projectileVisualSize = 0.18f;
    private bool awaitingAttackRelease;
    private EnemyController pendingAttackTarget;
    private uint pendingAttackSpawnVersion;
    private float attackReleaseTimeout;

    public void Initialize(TowerDefinition definition, ProjectilePool pool, BuildSlot slot)
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
        bool requiresDamage = attackBehaviour == null || attackBehaviour.RequiresDamage;
        if (float.IsNaN(definition.Damage) || float.IsInfinity(definition.Damage) ||
            definition.Damage < 0f || (requiresDamage && definition.Damage == 0f))
        {
            throw new System.ArgumentOutOfRangeException(nameof(definition.Damage),
                requiresDamage ? "Damage must be finite and greater than zero." :
                "Support tower damage must be finite and non-negative; it is ignored.");
        }
        if(definition.AttackInterval <= 0f)
        {
            throw new System.ArgumentOutOfRangeException(nameof(definition.AttackInterval), "Attack interval must be greater than zero.");
        }
        if((attackBehaviour == null || attackBehaviour.RequiresProjectile) &&
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
        if (slot == null)
        {
            throw new System.ArgumentNullException(nameof(slot));
        }
        if (samplesAntigenOnHit && antigenMemoryDuration <= 0f)
        {
            throw new System.InvalidOperationException("Antigen memory duration must be greater than zero.");
        }
        if (samplesAntigenOnHit && attackBehaviour != null)
        {
            throw new System.InvalidOperationException("Only the basic projectile tower can sample antigens.");
        }
        Definition = definition;
        BuildCostPaid = definition.BuildCost;
        UpgradeCostPaid = 0;
        IsUpgraded = false;
        projectilePool = pool;
        occupiedSlot = slot;
        attackCooldown = 0f;
        currentTarget = null;
        currentTargetSpawnVersion = 0;
        targetFilter = new ContactFilter2D();
        targetFilter.SetLayerMask(enemyLayerMask);
        targetFilter.useTriggers = true;
        animator = GetComponent<Animator>();
        bodyRenderer = GetComponent<SpriteRenderer>();
        if (bodyRenderer != null && GetComponent<TowerImpactRecoil>() == null) gameObject.AddComponent<TowerImpactRecoil>();
        if (aimVisualAtTarget) transform.rotation = Quaternion.identity;
        if (bodyRenderer != null)
        {
            bodyRenderer.flipX = false;
            bodyRenderer.flipY = false;
        }
        CancelPendingAttack();
        attackBehaviour?.Initialize(definition, enemyLayerMask);
        if (releaseOnAnimationEvent && animator != null && animator.runtimeAnimatorController != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }

    public bool TryUpgrade(int upgradeCostPaid)
    {
        if (Definition == null)
        {
            throw new System.InvalidOperationException("Tower is not initialized.");
        }
        if (IsUpgraded)
        {
            return false;
        }
        if (upgradeCostPaid <= 0)
        {
            throw new System.ArgumentOutOfRangeException(nameof(upgradeCostPaid));
        }

        UpgradeCostPaid = upgradeCostPaid;
        IsUpgraded = true;
        transform.localScale *= UpgradeVisualScale;
        return true;
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
            currentTargetSpawnVersion = 0;
        }
        if (currentTarget != null) AimAtTarget(currentTarget);

        attackCooldown -= Time.deltaTime;
        if (awaitingAttackRelease)
        {
            attackReleaseTimeout -= Time.deltaTime;
            if (attackReleaseTimeout <= 0f)
            {
                CancelPendingAttack();
            }
        }
        if (attackCooldown > 0f || awaitingAttackRelease)
        {
            return;
        }
        if (attackBehaviour != null &&
            (!attackBehaviour.isActiveAndEnabled || !attackBehaviour.CanAttack))
        {
            return;
        }

        // Reevaluate before every attack so a surviving target cannot keep priority
        // over another enemy that is now closer to reaching the end of its route.
        currentTarget = FindTarget();
        currentTargetSpawnVersion = currentTarget != null ? currentTarget.SpawnVersion : 0;
        if(currentTarget == null)
        {
            return;
        }

        attackCooldown = Definition.AttackInterval;
        AimAtTarget(currentTarget);
        if (releaseOnAnimationEvent && animator != null && animator.isActiveAndEnabled &&
            animator.runtimeAnimatorController != null)
        {
            pendingAttackTarget = currentTarget;
            pendingAttackSpawnVersion = currentTarget.SpawnVersion;
            awaitingAttackRelease = true;
            attackReleaseTimeout = 0.6f;
            animator.SetTrigger(AttackTrigger);
        }
        else
        {
            Attack(currentTarget);
            // Unity's missing/destroyed component check must use its overloaded null comparison.
            if (animator != null && animator.isActiveAndEnabled &&
                animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger(AttackTrigger);
            }
        }
    }

    // Invoked by the release pose in the attack clip.
    public void ReleaseAttack()
    {
        if (!awaitingAttackRelease)
        {
            return;
        }
        EnemyController target = pendingAttackTarget;
        uint spawnVersion = pendingAttackSpawnVersion;
        CancelPendingAttack();
        if (target == null || !target.isActiveAndEnabled || !target.CanBeTargeted ||
            target.SpawnVersion != spawnVersion)
        {
            return;
        }
        AimAtTarget(target);
        Attack(target);
    }

    private void AimAtTarget(EnemyController target)
    {
        if (aimVisualAtTarget)
            TopDownFacing.FaceDirection(transform, bodyRenderer, target.transform.position - transform.position);
    }

    private void CancelPendingAttack()
    {
        awaitingAttackRelease = false;
        pendingAttackTarget = null;
        pendingAttackSpawnVersion = 0;
        attackReleaseTimeout = 0f;
    }

    private void OnDisable()
    {
        CancelPendingAttack();
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
        if (currentTarget.SpawnVersion != currentTargetSpawnVersion)
        {
            return false;
        }
        if (!currentTarget.CanBeTargeted)
        {
            return false;
        }
        if (attackBehaviour != null && !attackBehaviour.CanTarget(currentTarget)) return false;
        return GetTargetCenterDistanceSquared(currentTarget) <=
               Definition.AttackRange * Definition.AttackRange;
    }

    private EnemyController FindTarget()
    {
        using (TargetingMarker.Auto())
        {
            int candidateCount = Physics2D.OverlapCircle(
                transform.position, Definition.AttackRange, targetFilter, targetCandidates);
            EnemyController bestTarget = null;
            float bestDistanceSquared = float.PositiveInfinity;
            float rangeSquared = Definition.AttackRange * Definition.AttackRange;
            for (int i = 0; i < candidateCount; i++)
            {
                Collider2D collider = targetCandidates[i];
                EnemyController candidate = collider.GetComponentInParent<EnemyController>();
                if (candidate == null || !candidate.CanBeTargeted)
                {
                    continue;
                }
                if (attackBehaviour != null && !attackBehaviour.CanTarget(candidate)) continue;

                // Physics overlap includes a collider when its edge enters the circle.
                // The preview measures the distance between the two object centers.
                float candidateDistanceSquared = GetTargetCenterDistanceSquared(candidate);
                if (candidateDistanceSquared > rangeSquared)
                {
                    continue;
                }
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
    }

    private float GetTargetCenterDistanceSquared(EnemyController target)
    {
        Vector2 offset = (Vector2)target.transform.position - (Vector2)transform.position;
        return offset.sqrMagnitude;
    }

    private bool IsCandidateBetter(
        EnemyController candidate,
        float candidateDistanceSquared,
        EnemyController currentBest,
        float currentBestDistanceSquared)
    {
        if (Definition.TargetingMode == TargetingMode.FarthestAlongPath)
        {
            // Different entrances can have routes of different lengths. Compare
            // remaining world distance, not waypoint indices or traveled percentage.
            float candidateRemaining = candidate.RemainingPathDistance;
            float currentRemaining = currentBest.RemainingPathDistance;
            if (!Mathf.Approximately(candidateRemaining, currentRemaining))
            {
                return candidateRemaining < currentRemaining;
            }
        }

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
            case TargetingMode.FarthestAlongPath:
                return candidateDistanceSquared < currentBestDistanceSquared;

            default:
                throw new System.ArgumentOutOfRangeException(nameof(Definition.TargetingMode));
        }
    }

    private void Attack(EnemyController target)
    {
        float damage = CurrentDamage;
        if (attackBehaviour != null)
        {
            if (!attackBehaviour.isActiveAndEnabled || !attackBehaviour.CanTarget(target)) return;
            attackBehaviour.Attack(target, damage, projectilePool);
            return;
        }

        using (SpawnProjectileMarker.Auto())
        {
            Vector3 muzzle = muzzleLocalOffset;
            if (aimVisualAtTarget)
            {
                // Convert world aim back to local space so body rotation is not applied twice.
                Vector3 direction = transform.InverseTransformDirection(target.transform.position - transform.position);
                direction.z = 0f;
                if (direction.sqrMagnitude > 0.0001f)
                    muzzle = direction.normalized * muzzleLocalOffset.magnitude;
            }
            Projectile projectile = projectilePool.Get(transform.TransformPoint(muzzle));
            if (projectileVisual != null)
            {
                projectile.SetVisual(projectileVisual, Color.white, projectileVisualSize);
            }
            projectile.Initialize(target, damage, 0f,
                samplesAntigenOnHit ? HandleAntigenSample : null);
            CellCombatFeedback.PrepareProjectile(projectile, CellImpactKind.Neutrophil);
            CellCombatFeedback.Release(CellImpactKind.Neutrophil, this, target.transform.position);
            CombatHitFeedback.Shot(projectile.transform.position);
        }
    }

    private void HandleAntigenSample(EnemyController target)
    {
        if (this == null || !isActiveAndEnabled || occupiedSlot == null ||
            occupiedSlot.CurrentTower != this || target == null || target.Definition == null)
        {
            return;
        }

        occupiedSlot.ShareAntigen(target.CurrentAntigenId, antigenMemoryDuration);
    }
}
