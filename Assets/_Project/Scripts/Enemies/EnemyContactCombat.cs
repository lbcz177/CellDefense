using System;
using UnityEngine;

// Movement gate uses the unit's actual route segment; it never seeks cells outside that route.
[DisallowMultipleComponent]
public sealed class EnemyContactCombat : MonoBehaviour
{
    private EnemyController enemy;
    private uint version;
    private PathFollower follower;
    private EnemyStunState stun;
    private Func<Vector3, TowerCombatState> findCell;
    private Func<bool> assaultActive;
    private Func<Vector3, bool> movementGate;
    private TowerCombatState target;
    private TowerCombatState bypassed;
    private float contactRadius;
    private float nextHitAt;
    private float damage;
    private float interval;
    private bool attackPrepared;
    public TowerCombatState Target => target;

    public void Initialize(EnemyController unit, float cellSize,
        Func<Vector3, TowerCombatState> lookup, Func<bool> inAssault)
    {
        ResetState();
        enemy = unit != null ? unit : throw new ArgumentNullException(nameof(unit));
        EnemyRangeQuery.RequirePositive(cellSize, nameof(cellSize));
        findCell = lookup ?? throw new ArgumentNullException(nameof(lookup));
        assaultActive = inAssault ?? throw new ArgumentNullException(nameof(inAssault));
        follower = GetComponent<PathFollower>();
        stun = GetComponent<EnemyStunState>();
        version = enemy.SpawnVersion;
        enemy.Exited += HandleExited;
        contactRadius = cellSize * 0.45f;
        damage = enemy.Definition.ContactDamage;
        interval = enemy.Definition.ContactAttackInterval;
        movementGate = TryMove;
        follower.BeforeMove = movementGate;
    }

    private bool IsCurrent => enemy != null && enemy.isActiveAndEnabled && enemy.CanBeTargeted &&
        enemy.SpawnVersion == version && assaultActive != null && assaultActive();

    private void Update()
    {
        if (Time.deltaTime > 0f) Tick(Time.time);
        else if (!IsCurrent) ReleaseTarget();
    }

    public void Tick(float now)
    {
        if (!IsCurrent)
        {
            ReleaseTarget();
            return;
        }
        if (target == null) return;
        if (!target.CanTakeContact)
        {
            ReleaseTarget();
            return;
        }
        if (stun != null && stun.IsStunned) return;
        enemy.FaceTowards(target.transform.position);
        if (!attackPrepared && now >= nextHitAt - 0.16f)
        {
            enemy.PlayContactAttackAnimation(target.transform.position);
            attackPrepared = true;
        }
        if (now < nextHitAt) return;
        nextHitAt = now + interval; // No accumulated burst after pause or a long frame.
        attackPrepared = false;
        target.TakeDamage(damage);
        if (target != null && !target.CanTakeContact) ReleaseTarget();
    }

    private bool TryMove(Vector3 nextPosition)
    {
        if (!IsCurrent) return true;
        if (target != null && target.CanTakeContact) return false;
        ReleaseTarget();
        // Each frame moves toward exactly one route node, including during large deltaTime.
        Vector3 node = follower.CurrentPath.GetPoint(follower.NextPointIndex).position;
        TowerCombatState candidate = findCell(node);
        if (candidate == null || !candidate.CanTakeContact)
        {
            bypassed = null;
            return true;
        }
        if (candidate == bypassed) return true;
        Vector3 from = transform.position;
        Vector3 segment = nextPosition - from;
        float lengthSquared = segment.sqrMagnitude;
        float t = lengthSquared > 0f ? Mathf.Clamp01(Vector3.Dot(candidate.transform.position - from,
            segment) / lengthSquared) : 0f;
        Vector3 closest = from + segment * t;
        if ((candidate.transform.position - closest).sqrMagnitude > contactRadius * contactRadius)
            return true;
        if (!candidate.TryEngage(enemy))
        {
            bypassed = candidate; // Overflow commits to passing this cell, even if a slot frees later.
            return true;
        }
        target = candidate;
        nextHitAt = Time.time + interval; // First hit after one complete attack interval.
        float distance = Vector3.Distance(from, candidate.transform.position);
        if (distance > contactRadius)
            transform.position = Vector3.MoveTowards(from, candidate.transform.position, distance - contactRadius);
        enemy.FaceTowards(candidate.transform.position);
        return false;
    }

    private void ReleaseTarget()
    {
        if (target != null)
        {
            target.Release(enemy, version);
            if (enemy != null) enemy.ResumeMovementAnimation();
        }
        attackPrepared = false;
        target = null;
    }

    public void ReleaseDefender(TowerCombatState defender)
    {
        if (target == defender) ReleaseTarget();
        if (bypassed == defender) bypassed = null;
    }

    public void ResetState()
    {
        ReleaseTarget();
        if (enemy != null) enemy.Exited -= HandleExited;
        bypassed = null;
        if (follower != null && follower.BeforeMove == movementGate) follower.BeforeMove = null;
        enemy = null;
        findCell = null;
        assaultActive = null;
        movementGate = null;
    }

    private void OnDisable() => ResetState();
    private void HandleExited(EnemyController unit, EnemyExitReason reason) => ResetState();
}

