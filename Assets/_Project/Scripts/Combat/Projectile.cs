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
    private uint targetSpawnVersion;
    private float damage;
    private float markDuration;
    private AntigenId markAntigen;
    private EnemyDamageKind damageKind;
    private Action<EnemyController> onHit;
    private bool isInitialized;
    private bool hasResolved;
    private ProjectilePool ownerPool;
    private SpriteRenderer visualRenderer;
    private Sprite originalSprite;
    private Color originalColor;
    private Vector3 originalScale;
    private bool visualCached;
    private bool hasVisualOverride;

    public void SetVisual(Sprite sprite, Color color, float size)
    {
        if (sprite == null || size <= 0f)
        {
            throw new ArgumentException("Projectile visual requires a sprite and positive size.");
        }
        if (!visualCached)
        {
            visualRenderer = GetComponent<SpriteRenderer>();
            if (visualRenderer == null)
            {
                throw new InvalidOperationException("Projectile visual requires a SpriteRenderer.");
            }
            originalSprite = visualRenderer.sprite;
            originalColor = visualRenderer.color;
            originalScale = transform.localScale;
            visualCached = true;
        }
        visualRenderer.sprite = sprite;
        visualRenderer.color = color;
        float extent = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        transform.localScale = Vector3.one * (size / Mathf.Max(extent, 0.0001f));
        hasVisualOverride = true;
    }

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
        Action<EnemyController> onHit, EnemyDamageKind kind = EnemyDamageKind.Normal)
    {
        if ((uint)kind > (uint)EnemyDamageKind.Neural)
            throw new ArgumentOutOfRangeException(nameof(kind));
        if (float.IsNaN(damage) || float.IsInfinity(damage) || damage <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(damage), "Damage must be finite and greater than zero.");
        }
        InitializeInternal(target, damage, markDuration, onHit);
        damageKind = kind;
    }

    // A support projectile applies a mark without sending zero damage to Health.
    public void InitializeMark(EnemyController target, float markDuration)
    {
        if (float.IsNaN(markDuration) || float.IsInfinity(markDuration) || markDuration <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(markDuration));
        }
        InitializeInternal(target, 0f, markDuration, null);
    }

    private void InitializeInternal(EnemyController target, float damage, float markDuration,
        Action<EnemyController> onHit)
    {
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
        if (float.IsNaN(markDuration) || float.IsInfinity(markDuration) || markDuration < 0f)
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
        targetSpawnVersion = target.SpawnVersion;
        this.damage = damage;
        this.markDuration = markDuration;
        markAntigen = target.CurrentAntigenId;
        damageKind = EnemyDamageKind.Normal;
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

        if (target == null || target.SpawnVersion != targetSpawnVersion ||
            !target.CanBeTargeted || !target.isActiveAndEnabled)
        {
            Finish();
            return;
        }

        Move();
    }

    private void Move()
    {
        if (hasVisualOverride)
        {
            Vector3 direction = target.transform.position - transform.position;
            if (direction.sqrMagnitude > Mathf.Epsilon)
            {
                transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            }
        }
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
        if (target == null || target.SpawnVersion != targetSpawnVersion ||
            !target.CanBeTargeted || !target.isActiveAndEnabled)
        {
            Finish();
            return;
        }
        onHit?.Invoke(target);
        if (target == null || target.SpawnVersion != targetSpawnVersion ||
            !target.isActiveAndEnabled || !target.CanBeTargeted)
        {
            Finish();
            return;
        }
        if (markDuration > 0f && target.CurrentAntigenId == markAntigen)
        {
            EnemyImmuneState immuneState = target.GetComponent<EnemyImmuneState>();
            if (immuneState == null)
            {
                throw new InvalidOperationException("Antibody target lost its EnemyImmuneState component.");
            }
            immuneState.ApplyMark(markDuration);
        }
        if (damage > 0f)
        {
            target.TakeDamage(new EnemyDamageContext(damage, damageKind));
        }
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
        if (visualCached)
        {
            visualRenderer.sprite = originalSprite;
            visualRenderer.color = originalColor;
            transform.localScale = originalScale;
        }
        hasVisualOverride = false;
        target = null;
        targetSpawnVersion = 0;
        damage = 0f;
        markDuration = 0f;
        markAntigen = default;
        damageKind = EnemyDamageKind.Normal;
        onHit = null;
        isInitialized = false;
        hasResolved = false;
    }
}
