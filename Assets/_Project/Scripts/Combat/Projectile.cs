using UnityEngine;
using System;
using Unity.Profiling;

public class Projectile : MonoBehaviour
{
    private static readonly ProfilerMarker ReturnProjectileMarker =
        new ProfilerMarker("CellDefense.Projectile.Return");

    [SerializeField, Min(0.01f)]
    private float moveSpeed = 6f;
    [SerializeField, Min(0.01f)]
    private float hitDistance = 0.1f;

    private EnemyController target;
    private float damage;
    private float markDuration;
    private Action<EnemyController> onHit;
    private bool isInitialized;
    private bool hasResolved;
    private ProjectilePool ownerPool;

    public void AssignPool(ProjectilePool pool)
    {
        if (pool == null)
        {
            throw new ArgumentNullException(nameof(pool));
        }
        if (ownerPool != null && ownerPool != pool)
        {
            throw new InvalidOperationException("Projectile already belongs to another pool.");
        }

        ownerPool = pool;
    }

    public void Initialize(EnemyController target, float damage)
    {
        Initialize(target, damage, 0f);
    }

    public void Initialize(EnemyController target, float damage, float markDuration)
    {
        Initialize(target, damage, markDuration, null);
    }

    public void Initialize(EnemyController target, float damage, float markDuration,
        Action<EnemyController> onHit)
    {
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
        if (damage <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(damage), "Damage must be greater than zero.");
        }
        if (markDuration < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(markDuration));
        }
        if (moveSpeed <= 0f)
        {
            throw new InvalidOperationException("Projectile move speed must be greater than zero.");
        }
        if (hitDistance <= 0f)
        {
            throw new InvalidOperationException("Projectile hit distance must be greater than zero.");
        }

        this.target = target;
        this.damage = damage;
        this.markDuration = markDuration;
        this.onHit = onHit;
        isInitialized = true;
        hasResolved = false;
    }

    private void Update()
    {
        if (!isInitialized || hasResolved)
        {
            return;
        }
        if (Time.deltaTime <= 0f)
        {
            return;
        }

        if (target == null || !target.CanBeTargeted || target.isActiveAndEnabled == false)
        {
            Finish();
            return;
        }

        Move();
    }

    private void Move()
    {
        transform.position = Vector3.MoveTowards(transform.position, target.transform.position, moveSpeed * Time.deltaTime);
        float distanceSquared = (target.transform.position - transform.position).sqrMagnitude;
        if (distanceSquared <= hitDistance * hitDistance)
        {
            Hit();
        }

    }

    private void Hit()
    {
        if (hasResolved)
        {
            return;
        }
        if (target == null || !target.CanBeTargeted || target.isActiveAndEnabled == false)
        {
            Finish();
            return;
        }
        onHit?.Invoke(target);
        if (markDuration > 0f)
        {
            EnemyImmuneState immuneState = target.GetComponent<EnemyImmuneState>();
            if (immuneState == null)
            {
                throw new InvalidOperationException("Antibody target lost its EnemyImmuneState component.");
            }
            immuneState.ApplyMark(markDuration);
        }
        target.TakeDamage(damage);
        Finish();
    }

    private void Finish()
    {
        if (hasResolved)
        {
            return;
        }

        hasResolved = true;
        if (ownerPool == null)
        {
            throw new InvalidOperationException("Projectile does not belong to a pool.");
        }

        using (ReturnProjectileMarker.Auto())
        {
            ownerPool.Release(this);
        }
    }

    public void ResetForPool()
    {
        target = null;
        damage = 0f;
        markDuration = 0f;
        onHit = null;
        isInitialized = false;
        hasResolved = false;
    }
}
