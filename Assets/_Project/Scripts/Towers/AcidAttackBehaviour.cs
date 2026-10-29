using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(TowerController))]
public class AcidAttackBehaviour : TowerAttackBehaviour
{
    [SerializeField, Min(0.01f)] private float acidRadius = 1.25f;
    [SerializeField, Min(0.01f)] private float acidDuration = 3f;
    [SerializeField, Min(0.01f)] private float damagePerSecond = 10f;
    [SerializeField, Min(0.01f)] private float tickInterval = 0.25f;

    [Header("Acid Field Visual")]
    [SerializeField] private Color acidColor = new Color(0.35f, 0.95f, 0.12f, 0.22f);
    [SerializeField] private Color boundaryColor = new Color(0.6f, 1f, 0.2f, 0.75f);

    private struct AcidField
    {
        public Vector2 Center;
        public float Radius;
        public float ExpiresAt;
        public float LastTickAt;
        public float DamagePerSecond;
        public AcidFieldVisual Visual;
    }

    private struct DamagePacket
    {
        public EnemyTargetSnapshot Target;
        public float Amount;
    }

    private TowerController tower;
    private EnemyRangeQuery query;
    private readonly List<AcidField> fields = new List<AcidField>(8);
    private readonly Dictionary<EnemyController, DamagePacket> damageByTarget =
        new Dictionary<EnemyController, DamagePacket>();
    private readonly List<DamagePacket> pendingDamage = new List<DamagePacket>(32);
    private float tickCooldown;
    private uint activationVersion;
    private Transform visualRoot;
    private Mesh visualMesh;
    private Material visualMaterial;
    private readonly Stack<AcidFieldVisual> inactiveVisuals = new Stack<AcidFieldVisual>();

    public override bool RequiresProjectile => true;
    public override bool CanAttack => isActiveAndEnabled && query != null &&
        tower != null && tower.isActiveAndEnabled;
    public int ActiveFieldCount => fields.Count;

    public override void Initialize(TowerDefinition definition, LayerMask enemyLayers)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        EnemyRangeQuery.RequirePositive(definition.Damage, nameof(definition.Damage));
        EnemyRangeQuery.RequirePositive(acidRadius, nameof(acidRadius));
        EnemyRangeQuery.RequirePositive(acidDuration, nameof(acidDuration));
        EnemyRangeQuery.RequirePositive(damagePerSecond, nameof(damagePerSecond));
        EnemyRangeQuery.RequirePositive(tickInterval, nameof(tickInterval));
        if (tickInterval > acidDuration)
            throw new InvalidOperationException("Acid tick interval must not exceed its duration.");

        tower = GetComponent<TowerController>();
        query = new EnemyRangeQuery(enemyLayers);
        CancelEffects();
        EnsureVisualPool();
    }

    public override void Attack(EnemyController target, float damage, ProjectilePool projectilePool)
    {
        if (!CanAttack || target == null || !target.isActiveAndEnabled || !target.CanBeTargeted) return;
        if (projectilePool == null) throw new ArgumentNullException(nameof(projectilePool));
        EnemyRangeQuery.RequirePositive(damage, nameof(damage));

        // Keep the release-time upgrade multiplier even if the tower changes during flight.
        float rate = damagePerSecond * (damage / tower.Definition.Damage);
        uint castVersion = activationVersion;
        Projectile projectile = projectilePool.Get(transform.position);
        projectile.Initialize(target, damage, 0f,
            hit => CreateAcidField(hit, rate, castVersion), EnemyDamageKind.Acid);
        CellCombatFeedback.PrepareProjectile(projectile, CellImpactKind.Acid);
        CellCombatFeedback.Release(CellImpactKind.Acid, this, target.transform.position);
        CombatHitFeedback.Shot(projectile.transform.position);
    }

    private void CreateAcidField(EnemyController target, float rate, uint castVersion)
    {
        if (this == null || !CanAttack || castVersion != activationVersion ||
            target == null || !target.isActiveAndEnabled || !target.CanBeTargeted) return;

        // Projectile invokes this callback before direct damage, so lethal hits still leave acid.
        Vector2 center = target.transform.position;
        AcidFieldVisual visual = inactiveVisuals.Count > 0 ? inactiveVisuals.Pop() :
            new AcidFieldVisual(visualRoot, visualMesh, visualMaterial);
        visual.Show(center, acidRadius, acidDuration, acidColor, boundaryColor);
        fields.Add(new AcidField
        {
            Center = center,
            Radius = acidRadius,
            ExpiresAt = Time.time + acidDuration,
            LastTickAt = Time.time,
            DamagePerSecond = rate,
            Visual = visual
        });
    }

    private void Update()
    {
        if (query == null) return;
        if (tower == null || !tower.isActiveAndEnabled)
        {
            CancelEffects();
            return;
        }
        if (Time.deltaTime <= 0f || fields.Count == 0) return;

        // Visuals expire at the field deadline, independently of the damage sampling interval.
        float now = Time.time;
        for (int i = 0; i < fields.Count; i++) fields[i].Visual.Refresh(now);

        tickCooldown -= Time.deltaTime;
        if (tickCooldown > 0f) return;
        ResolveFields();
        tickCooldown = tickInterval;
    }

    private void ResolveFields()
    {
        float now = Time.time;
        damageByTarget.Clear();
        pendingDamage.Clear();
        for (int i = fields.Count - 1; i >= 0; i--)
        {
            AcidField field = fields[i];
            float tickEnd = Mathf.Min(now, field.ExpiresAt);
            float elapsed = Mathf.Max(0f, tickEnd - field.LastTickAt);
            if (elapsed > 0f)
            {
                query.Collect(field.Center, field.Radius);
                for (int targetIndex = 0; targetIndex < query.Targets.Count; targetIndex++)
                {
                    EnemyTargetSnapshot target = query.Targets[targetIndex];
                    float amount = field.DamagePerSecond * elapsed;
                    if (damageByTarget.TryGetValue(target.Enemy, out DamagePacket packet) &&
                        packet.Target.SpawnVersion == target.SpawnVersion)
                    {
                        packet.Amount += amount;
                    }
                    else
                    {
                        packet = new DamagePacket { Target = target, Amount = amount };
                    }
                    damageByTarget[target.Enemy] = packet;
                }
            }

            if (now >= field.ExpiresAt)
            {
                ReturnVisual(field.Visual);
                fields.RemoveAt(i);
            }
            else
            {
                field.LastTickAt = tickEnd;
                fields[i] = field;
            }
        }

        // Build the batch before damage callbacks can return enemies or disable this tower.
        foreach (DamagePacket packet in damageByTarget.Values) pendingDamage.Add(packet);
        damageByTarget.Clear();
        for (int i = 0; i < pendingDamage.Count; i++)
        {
            if (!CanAttack) break;
            DamagePacket packet = pendingDamage[i];
            if (packet.Target.IsValid && packet.Amount > 0f)
                packet.Target.Enemy.TakeDamage(new EnemyDamageContext(packet.Amount, EnemyDamageKind.Acid));
        }
        pendingDamage.Clear();
    }

    private void CancelEffects()
    {
        activationVersion++;
        for (int i = 0; i < fields.Count; i++) ReturnVisual(fields[i].Visual);
        fields.Clear();
        damageByTarget.Clear();
        pendingDamage.Clear();
        query?.Clear();
        tickCooldown = 0f;
    }

    private void OnDisable() => CancelEffects();

    private void EnsureVisualPool()
    {
        if (visualRoot != null) return;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) throw new InvalidOperationException("Acid visuals require the Sprites/Default shader.");
        visualMaterial = new Material(shader) { name = "Acid Ground Material" };
        visualMaterial.mainTexture = Texture2D.whiteTexture;
        visualMesh = AcidFieldVisual.CreateUnitMesh();
        // Keep world-sized circles independent of the tower's small sprite scale.
        GameObject root = new GameObject($"Acid Fields ({name})");
        SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        visualRoot = root.transform;
        for (int i = 0; i < 2; i++)
            inactiveVisuals.Push(new AcidFieldVisual(visualRoot, visualMesh, visualMaterial));
    }

    private void ReturnVisual(AcidFieldVisual visual)
    {
        if (visual == null) return;
        visual.Hide();
        inactiveVisuals.Push(visual);
    }

    private void OnDestroy()
    {
        CancelEffects();
        inactiveVisuals.Clear();
        if (visualRoot != null) Destroy(visualRoot.gameObject);
        if (visualMesh != null) Destroy(visualMesh);
        if (visualMaterial != null) Destroy(visualMaterial);
    }
}
