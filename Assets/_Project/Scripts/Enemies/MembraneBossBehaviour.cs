using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MembraneBossBehaviour : BossAbilityBehaviour
{
    private EnemyRangeQuery query;
    private readonly List<EnemyTargetSnapshot> nearest = new List<EnemyTargetSnapshot>(4);
    private EnemyVisualState visual;
    private float nextShieldAt;
    private float nextHealAt;
    private bool shieldWarning;
    public override string WarningLabel => shieldWarning ? "即将：护盾脉冲" : string.Empty;

    public override void Initialize(EnemyDefinition definition)
    {
        query = new EnemyRangeQuery(1 << gameObject.layer);
        visual = GetComponent<EnemyVisualState>();
        base.Initialize(definition);
        nextShieldAt = Time.time + 6f;
        nextHealAt = Time.time + 2f;
    }
    public override void ResetState()
    {
        shieldWarning = false;
        nextShieldAt = nextHealAt = 0f;
        nearest.Clear();
        query?.Clear();
    }
    public override void ExecutePhase(int index) { }

    private void Update()
    {
        if (Time.deltaTime > 0f) Tick(Time.time);
    }

    public void Tick(float now)
    {
        if (!IsCurrent)
        {
            shieldWarning = false;
            return;
        }
        if (!shieldWarning && now >= nextShieldAt - 0.8f)
        {
            shieldWarning = true;
            if (visual != null) visual.ShowPulse(new Color(0.3f, 0.65f, 1f, 0.85f), 2f, 0.8f);
        }
        if (now >= nextShieldAt)
        {
            shieldWarning = false;
            nextShieldAt = now + 6f;
            ApplySupport(true);
        }
        if (IsCurrent && now >= nextHealAt)
        {
            nextHealAt = now + 2f;
            ApplySupport(false);
        }
    }

    private void ApplySupport(bool shields)
    {
        if (shields) Enemy.GrantShield(80f);
        else Enemy.Heal(5f);
        if (!IsCurrent) return;
        CollectNearest();
        for (int i = 0; i < nearest.Count && IsCurrent; i++)
        {
            EnemyTargetSnapshot target = nearest[i];
            if (!target.IsValid) continue;
            if (shields) target.Enemy.GrantShield(40f);
            else target.Enemy.Heal(5f);
        }
        if (IsCurrent && visual != null)
            visual.ShowPulse(shields ? new Color(0.3f, 0.65f, 1f, 0.85f) :
                new Color(0.3f, 1f, 0.5f, 0.8f), 2f, 0.4f);
    }

    private void CollectNearest()
    {
        nearest.Clear();
        Vector2 center = Enemy.transform.position;
        query.Collect(center, 2f);
        for (int i = 0; i < query.Targets.Count; i++)
        {
            EnemyTargetSnapshot candidate = query.Targets[i];
            if (candidate.Enemy == Enemy || candidate.Enemy.Definition == null ||
                candidate.Enemy.Definition.IsBoss) continue;
            float distance = (candidate.Position - center).sqrMagnitude;
            int insert = 0;
            while (insert < nearest.Count)
            {
                float other = (nearest[insert].Position - center).sqrMagnitude;
                if (distance < other || (Mathf.Approximately(distance, other) &&
                    candidate.Enemy.GetInstanceID() < nearest[insert].Enemy.GetInstanceID())) break;
                insert++;
            }
            if (insert >= 3) continue;
            nearest.Insert(insert, candidate);
            if (nearest.Count > 3) nearest.RemoveAt(3);
        }
    }
}
