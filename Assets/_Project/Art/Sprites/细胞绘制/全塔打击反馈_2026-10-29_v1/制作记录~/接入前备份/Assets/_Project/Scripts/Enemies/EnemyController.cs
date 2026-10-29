using UnityEngine;
using System;

public class EnemyController : MonoBehaviour
{

    [SerializeField] private PathFollower pathFollower;
    [SerializeField] private bool playCoreAttackAnimation;
    [SerializeField] private bool rotateBodyWithDirection;
    private static readonly int AttackTrigger = Animator.StringToHash("Attack");
    private Animator animator;
    private SpriteHitFlash hitFlash;
    private SpriteRenderer bodyRenderer;
    private bool isAttackingCore;
    private float coreAttackTimeout;
    public event Action<EnemyController, EnemyExitReason> Exited;
    public event Action<EnemySpawnRequest> SummonRequested;
    private bool hasResolved;
    private Health health;//接入health引用
    private EnemySlowState slowState;
    private EnemyStunState stunState;
    private EnemyImmuneState immuneState;
    private EnemyArmorState armorState;
    private EnemyShieldState shieldState;
    private EnemyBehaviour[] behaviours;
    private BossPhaseController bossPhases;
    private BossAbilityBehaviour bossAbility;
    private bool isInitialized;
    public EnemyDefinition Definition { get; private set; }
    public uint SpawnVersion { get; private set; }
    public AntigenId CurrentAntigenId { get; private set; }
    public float CurrentHealth => health != null ? health.CurrentHealth : 0f;
    public float MaxHealth => health != null ? health.MaxHealth : 0f;
    public float CurrentShield => shieldState != null && shieldState.isActiveAndEnabled ? shieldState.Current : 0f;
    public float ShieldCapacity => shieldState != null ? shieldState.Capacity : 0f;
    public bool HasShield => CurrentShield > 0f;
    public bool ContainsVisualPoint(Vector3 point) => bodyRenderer != null && bodyRenderer.bounds.Contains(point);
    public bool IsArmorBroken => armorState != null && armorState.IsBroken;
    public string BossPhaseLabel => bossPhases != null ? bossPhases.PhaseLabel : string.Empty;
    public string AbilityWarning => bossPhases != null && !string.IsNullOrEmpty(bossPhases.WarningLabel)
        ? bossPhases.WarningLabel : bossAbility != null ? bossAbility.WarningLabel : string.Empty;
    public float PathProgress => pathFollower.Progress;
    public float RemainingPathDistance => pathFollower.RemainingDistance;
    
    public bool CanBeTargeted => isInitialized && !hasResolved && !isAttackingCore && health != null &&
        !health.IsDead && health.CurrentHealth > 0f;
    

    void Awake()
    {
        health = GetComponent<Health>();
        pathFollower = GetComponent<PathFollower>();
        animator = GetComponent<Animator>();
        hitFlash = GetComponent<SpriteHitFlash>();
        bodyRenderer = GetComponent<SpriteRenderer>();
        slowState = GetComponent<EnemySlowState>();
        stunState = GetComponent<EnemyStunState>();
        immuneState = GetComponent<EnemyImmuneState>();
        armorState = GetComponent<EnemyArmorState>();
        shieldState = GetComponent<EnemyShieldState>();
        behaviours = GetComponents<EnemyBehaviour>();
        bossPhases = GetComponent<BossPhaseController>();
        bossAbility = GetComponent<BossAbilityBehaviour>();
    }

    public void Initialize(WaypointPath path, EnemyDefinition definition)
    {
        Initialize(path, definition, 1f, null);
    }

    public void Initialize(WaypointPath path, EnemyDefinition definition, float sceneSpeedMultiplier,
        EnemySpawnRequest? spawn = null)
    {
        if(pathFollower == null)
        {
            throw new ArgumentNullException(nameof(pathFollower));
        }
        if(definition == null)
        {
            throw new ArgumentNullException(nameof(definition));
        }
        if(path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }
        definition.Validate();
        EnemyRangeQuery.RequirePositive(sceneSpeedMultiplier, nameof(sceneSpeedMultiplier));
        if (definition.ArmorReduction > 0f && armorState == null)
            throw new InvalidOperationException("Armored enemies require EnemyArmorState.");
        if (definition.InitialShield > 0f && shieldState == null)
            throw new InvalidOperationException("Shielded enemies require EnemyShieldState.");
        EnemyContactCombat contact = GetComponent<EnemyContactCombat>();
        if (contact != null) contact.ResetState();
        isInitialized = false;
        Definition = definition;
        SpawnVersion++;
        CurrentAntigenId = spawn.HasValue && spawn.Value.Antigen.HasValue
            ? spawn.Value.Antigen.Value : definition.AntigenId;
        slowState = GetComponent<EnemySlowState>();
        if (slowState != null) slowState.Initialize(definition);
        stunState = GetComponent<EnemyStunState>();
        if (stunState != null) stunState.Initialize(definition);
        immuneState = GetComponent<EnemyImmuneState>();
        if (immuneState != null) immuneState.ResetState();
        if (armorState != null) armorState.Initialize(definition);
        if (shieldState != null) shieldState.Initialize(definition.InitialShield);
        hasResolved = false;
        isAttackingCore = false;
        coreAttackTimeout = 0f;
        if (rotateBodyWithDirection)
        {
            transform.rotation = Quaternion.identity;
            if (bodyRenderer != null)
            {
                bodyRenderer.flipX = false;
                bodyRenderer.flipY = false;
            }
        }
        if (playCoreAttackAnimation && animator != null && animator.runtimeAnimatorController != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
        health.Initialize(definition.MaxHealth);
        if (spawn.HasValue)
            pathFollower.InitializeAt(path, spawn.Value.Position, spawn.Value.NextPointIndex);
        else pathFollower.Initialize(path);
        pathFollower.SetBaseSpeed(definition.BaseMoveSpeed);
        pathFollower.SetSpeedMultiplier(sceneSpeedMultiplier);
        foreach (EnemyBehaviour behaviour in behaviours)
            if (behaviour != null && behaviour.isActiveAndEnabled) behaviour.Initialize(definition);
        isInitialized = true;
        // Reused units and summons take their new route's direction immediately.
        for (int i = pathFollower.NextPointIndex; rotateBodyWithDirection && i < path.PointCount; i++)
        {
            Vector3 direction = path.GetPoint(i).position - transform.position;
            if (direction.sqrMagnitude <= 0.000001f) continue;
            FaceMovementDirection(direction);
            break;
        }
        pathFollower.Begin();
        
    }

    private void OnEnable()
    {
        pathFollower.ReachedEnd += HandleReachedEnd;
        pathFollower.Moved += FaceMovementDirection;
        health.Died += HandleDeath;
    }

    private void OnDisable()
    {
        isInitialized = false;
        pathFollower.ReachedEnd -= HandleReachedEnd;
        pathFollower.Moved -= FaceMovementDirection;
        health.Died -= HandleDeath;
        isAttackingCore = false;
        coreAttackTimeout = 0f;
    }

    private void HandleReachedEnd()
    {
        if (hasResolved || isAttackingCore)
        {
            return;
        }
        if (playCoreAttackAnimation && animator != null && animator.isActiveAndEnabled &&
            animator.runtimeAnimatorController != null)
        {
            pathFollower.Stop();
            isAttackingCore = true;
            coreAttackTimeout = 0.75f;
            animator.SetTrigger(AttackTrigger);
            return;
        }
        TryExit(EnemyExitReason.Leaked);
    }

    // Contact damage keeps its own interval; this starts the visual anticipation before contact.
    public void PlayContactAttackAnimation(Vector3 targetPosition)
    {
        if (!isInitialized || hasResolved || isAttackingCore) return;
        FaceTowards(targetPosition);
        if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
        {
            animator.ResetTrigger(AttackTrigger);
            animator.Play("Attack", 0, 0f);
        }
    }

    public void FaceTowards(Vector3 worldPosition)
    {
        FaceMovementDirection(worldPosition - transform.position);
    }

    private void FaceMovementDirection(Vector3 direction)
    {
        if (rotateBodyWithDirection) TopDownFacing.FaceDirection(transform, bodyRenderer, direction);
    }

    public void ResumeMovementAnimation()
    {
        if (!isInitialized || hasResolved || isAttackingCore) return;
        if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
        {
            animator.ResetTrigger(AttackTrigger);
            animator.Play("Move", 0, 0f);
        }
    }

    private void Update()
    {
        if (!isAttackingCore || Time.deltaTime <= 0f)
        {
            return;
        }
        coreAttackTimeout -= Time.deltaTime;
        if (coreAttackTimeout <= 0f)
        {
            CompleteCoreAttack();
        }
    }

    // Invoked after the contact pose has appeared; a pooled spawn can resolve only once.
    public void CompleteCoreAttack()
    {
        if (isAttackingCore && !hasResolved)
        {
            CombatHitFeedback.CoreImpact(transform.position);
            TryExit(EnemyExitReason.Leaked);
        }
    }

    private void HandleDeath()
    {
        TryExit(EnemyExitReason.Killed);
    }

    private bool TryExit(EnemyExitReason reason)
    {
        if (hasResolved)
        {
            return false;
        }
        hasResolved = true;
        isAttackingCore = false;
        pathFollower.Stop();
        Exited?.Invoke(this, reason);
        return true;
    }

    public void Clear()
    {
        TryExit(EnemyExitReason.Cleared);
    }

    [ContextMenu("Test Take Damage")]
    void TestTakeDamage()
    {
        TakeDamage(50f);
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(new EnemyDamageContext(amount));
    }

    public void TakeDamage(EnemyDamageContext damage)
    {
        EnemyRangeQuery.RequirePositive(damage.Amount, nameof(damage.Amount));
        if (CanBeTargeted && isActiveAndEnabled)
        {
            uint version = SpawnVersion;
            hitFlash?.Play();
            CombatHitFeedback.Hit(bodyRenderer != null ? bodyRenderer.bounds.center : transform.position);
            if (damage.Kind == EnemyDamageKind.Acid && armorState != null) armorState.BreakArmor();
            float reduction = armorState != null ? armorState.DamageMultiplier : 1f;
            float multiplier = immuneState != null ? immuneState.DamageTakenMultiplier : 1f;
            float remaining = damage.Amount * reduction * multiplier;
            if (shieldState != null) remaining = shieldState.Absorb(remaining);
            if (remaining > 0f && SpawnVersion == version && CanBeTargeted && isActiveAndEnabled)
                health.TakeDamage(remaining);
        }
    }

    public bool CanEngulf(float threshold) => CanBeTargeted && isActiveAndEnabled &&
        Definition != null && Definition.CanBeEngulfed && !Definition.IsBoss && !HasShield &&
        CurrentHealth <= threshold;

    public bool TryEngulf(float threshold)
    {
        if (float.IsNaN(threshold) || float.IsInfinity(threshold) || threshold < 0f)
            throw new ArgumentOutOfRangeException(nameof(threshold));
        if (!CanEngulf(threshold)) return false;
        // Bypass mitigation while preserving Health.Died -> Exited -> reward and pooling.
        health.TakeDamage(health.CurrentHealth);
        return true;
    }

    public float Heal(float amount) => CanBeTargeted && isActiveAndEnabled ? health.Heal(amount) : 0f;

    public void GrantShield(float amount)
    {
        if (!CanBeTargeted || !isActiveAndEnabled) return;
        if (shieldState == null) throw new InvalidOperationException("Support targets require EnemyShieldState.");
        shieldState.ApplyAtLeast(amount);
    }

    public void ChangeAntigen(AntigenId antigen)
    {
        if (!CanBeTargeted || !isActiveAndEnabled) return;
        if (!Enum.IsDefined(typeof(AntigenId), antigen)) throw new ArgumentOutOfRangeException(nameof(antigen));
        CurrentAntigenId = antigen;
        if (immuneState != null) immuneState.ResetState();
    }

    public void SetAbilitySpeedMultiplier(float multiplier) => pathFollower.SetAbilitySpeedMultiplier(multiplier);

    public void RequestSummon(EnemyDefinition definition, int count, bool inheritAntigen)
    {
        if (!CanBeTargeted || !isActiveAndEnabled) return;
        if (definition == null || definition.EnemyPrefab == null || count < 1)
            throw new ArgumentException("Summons require a definition, prefab and positive count.");
        SummonRequested?.Invoke(new EnemySpawnRequest(this, definition, count, inheritAntigen));
    }
}
