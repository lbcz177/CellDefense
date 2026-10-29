using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TowerController))]
public class NeuralPulseAttackBehaviour : TowerAttackBehaviour
{
    [SerializeField, Min(1)] private int maxTargets = 3;
    [SerializeField, Min(0.01f)] private float chainRadius = 2.5f;
    [SerializeField, Range(0.01f, 1f)] private float damageRetention = 0.8f;
    [SerializeField, Min(0f)] private float stunDuration = 0.2f;
    [Header("Chain Feedback")]
    [SerializeField] private Color pulseColor = new Color(0.45f, 0.9f, 1f, 1f);
    [SerializeField, Min(0.01f)] private float pulseVisibleDuration = 0.15f;
    private LineRenderer pulse;
    private Material pulseMaterial;
    private float pulseUntil;
    private TowerController tower;
    private EnemyRangeQuery query;
    private readonly List<EnemyTargetSnapshot> chain = new List<EnemyTargetSnapshot>(8);
    private readonly HashSet<EnemyController> selectedEnemies = new HashSet<EnemyController>();
    private bool reportedMissingStunState;

    public override bool CanAttack => isActiveAndEnabled && query != null &&
        tower != null && tower.isActiveAndEnabled;

    public override void Initialize(TowerDefinition definition, LayerMask enemyLayers)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        if (maxTargets < 1) throw new ArgumentOutOfRangeException(nameof(maxTargets));
        EnemyRangeQuery.RequirePositive(chainRadius, nameof(chainRadius));
        EnemyRangeQuery.RequirePositive(damageRetention, nameof(damageRetention));
        EnemyRangeQuery.RequirePositive(pulseVisibleDuration, nameof(pulseVisibleDuration));
        if (damageRetention > 1f) throw new ArgumentOutOfRangeException(nameof(damageRetention));
        if (float.IsNaN(stunDuration) || float.IsInfinity(stunDuration) || stunDuration < 0f)
            throw new ArgumentOutOfRangeException(nameof(stunDuration));

        tower = GetComponent<TowerController>();
        query = new EnemyRangeQuery(enemyLayers);
        ClearChain();
    }

    public override void Attack(EnemyController target, float damage, ProjectilePool projectilePool)
    {
        if (!CanAttack || target == null || !target.isActiveAndEnabled || !target.CanBeTargeted) return;
        EnemyRangeQuery.RequirePositive(damage, nameof(damage));
        BuildChain(target);
        ShowChain();

        // Capture the complete chain before a lethal hit can recycle its origin.
        for (int i = 0; i < chain.Count; i++)
        {
            if (!CanAttack) break;
            EnemyTargetSnapshot hit = chain[i];
            if (!hit.IsValid) continue;
            float hitDamage = damage * Mathf.Pow(damageRetention, i);
            if (hitDamage <= 0f) break;

            if (stunDuration > 0f)
            {
                EnemyStunState stun = hit.Enemy.GetComponent<EnemyStunState>();
                if (stun != null && stun.isActiveAndEnabled)
                    stun.TryStun(stunDuration);
                else if (!reportedMissingStunState)
                {
                    Debug.LogError("Neural pulse targets need an enabled EnemyStunState for their brief stun.", hit.Enemy);
                    reportedMissingStunState = true;
                }
            }
            hit.Enemy.TakeDamage(new EnemyDamageContext(hitDamage, EnemyDamageKind.Neural));
        }
        chain.Clear();
        selectedEnemies.Clear();
    }

    private void BuildChain(EnemyController firstTarget)
    {
        chain.Clear();
        selectedEnemies.Clear();
        chain.Add(new EnemyTargetSnapshot(firstTarget));
        selectedEnemies.Add(firstTarget);
        while (chain.Count < maxTargets)
        {
            Vector2 origin = chain[chain.Count - 1].Position;
            query.Collect(origin, chainRadius);
            EnemyTargetSnapshot best = default;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < query.Targets.Count; i++)
            {
                EnemyTargetSnapshot candidate = query.Targets[i];
                if (selectedEnemies.Contains(candidate.Enemy)) continue;
                float distance = (candidate.Position - origin).sqrMagnitude;
                if (best.Enemy == null || distance < bestDistance ||
                    (Mathf.Approximately(distance, bestDistance) &&
                     candidate.Enemy.GetInstanceID() < best.Enemy.GetInstanceID()))
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }
            if (best.Enemy == null) break;
            chain.Add(best);
            selectedEnemies.Add(best.Enemy);
        }
    }

    private void ClearChain()
    {
        chain.Clear();
        selectedEnemies.Clear();
        query?.Clear();
        reportedMissingStunState = false;
        if (pulse != null) pulse.enabled = false;
    }

    private void OnDisable() => ClearChain();

    private void ShowChain()
    {
        if (pulse == null)
        {
            GameObject obj = new GameObject("Neural Chain");
            obj.transform.SetParent(transform, false);
            pulse = obj.AddComponent<LineRenderer>();
            pulseMaterial = new Material(Shader.Find("Sprites/Default")) { name = "Neural Chain" };
            pulse.sharedMaterial = pulseMaterial;
            pulse.useWorldSpace = true;
            pulse.widthMultiplier = 0.05f;
            SpriteRenderer body = GetComponent<SpriteRenderer>();
            if (body != null)
            {
                pulse.sortingLayerID = body.sortingLayerID;
                pulse.sortingOrder = body.sortingOrder + 2;
            }
        }
        pulse.positionCount = chain.Count + 1;
        pulse.SetPosition(0, transform.position);
        for (int i = 0; i < chain.Count; i++) pulse.SetPosition(i + 1, chain[i].Position);
        pulse.startColor = pulse.endColor = pulseColor;
        pulseUntil = Time.time + pulseVisibleDuration;
        pulse.enabled = true;
    }

    private void Update()
    {
        if (pulse == null || !pulse.enabled) return;
        if (Time.time >= pulseUntil) pulse.enabled = false;
        else
        {
            Color color = pulseColor;
            color.a *= Mathf.Clamp01((pulseUntil - Time.time) / pulseVisibleDuration);
            pulse.startColor = pulse.endColor = color;
        }
    }

    private void OnDestroy()
    {
        if (pulseMaterial == null) return;
        if (Application.isPlaying) Destroy(pulseMaterial);
        else DestroyImmediate(pulseMaterial);
    }
}
