using System;
using UnityEngine;

[Serializable]
public sealed class BossPhaseStep
{
    [Range(0f, 1f)] public float healthThreshold = 1f;
    [Min(0f)] public float warningDuration;
    public string label;
    public Sprite sprite;
}

[DisallowMultipleComponent]
public sealed class BossPhaseController : EnemyBehaviour
{
    [SerializeField] private BossPhaseStep[] steps;
    private BossAbilityBehaviour ability;
    private EnemyVisualState visual;
    private int nextIndex;
    private bool pending;
    private float readyAt;
    private float lowestFraction = 1f;
    private Health watchedHealth;
    public int PhaseIndex { get; private set; } = -1;
    public string PhaseLabel => PhaseIndex >= 0 ? steps[PhaseIndex].label : string.Empty;
    public string WarningLabel => pending ? "即将：" + steps[nextIndex].label : string.Empty;

    public override void Initialize(EnemyDefinition definition)
    {
        if (steps == null || steps.Length == 0) throw new ArgumentException("Assign Boss phase steps.");
        float previous = 2f;
        foreach (BossPhaseStep step in steps)
        {
            if (step == null || float.IsNaN(step.healthThreshold) || step.healthThreshold < 0f ||
                step.healthThreshold > 1f || step.healthThreshold >= previous ||
                float.IsNaN(step.warningDuration) || float.IsInfinity(step.warningDuration) ||
                step.warningDuration < 0f)
                throw new ArgumentException("Boss phases require decreasing thresholds and finite warning times.");
            previous = step.healthThreshold;
        }
        BossAbilityBehaviour[] abilities = GetComponents<BossAbilityBehaviour>();
        if (abilities.Length != 1) throw new ArgumentException("A Boss requires exactly one ability behaviour.");
        ability = abilities[0];
        visual = GetComponent<EnemyVisualState>();
        if (watchedHealth != null) watchedHealth.HealthChanged -= TrackLowestHealth;
        base.Initialize(definition);
        watchedHealth = GetComponent<Health>();
        watchedHealth.HealthChanged += TrackLowestHealth;
        TrackLowestHealth(watchedHealth.CurrentHealth, watchedHealth.MaxHealth);
    }

    public override void ResetState()
    {
        nextIndex = 0;
        pending = false;
        readyAt = 0f;
        PhaseIndex = -1;
        lowestFraction = 1f;
    }

    private void TrackLowestHealth(float current, float maximum)
    {
        if (maximum > 0f) lowestFraction = Mathf.Min(lowestFraction, current / maximum);
    }

    protected override void OnDisable()
    {
        if (watchedHealth != null) watchedHealth.HealthChanged -= TrackLowestHealth;
        watchedHealth = null;
        base.OnDisable();
    }

    private void Update()
    {
        if (Time.deltaTime > 0f) Tick(Time.time);
    }

    // Explicit clock makes threshold jumps and canceled warnings independently reviewable.
    public void Tick(float now)
    {
        if (!IsCurrent || ability == null || !ability.isActiveAndEnabled)
        {
            pending = false;
            return;
        }
        while (nextIndex < steps.Length && IsCurrent)
        {
            if (!pending)
            {
                if (lowestFraction > steps[nextIndex].healthThreshold) return;
                pending = true;
                readyAt = now + steps[nextIndex].warningDuration;
                if (steps[nextIndex].warningDuration > 0f && visual != null)
                    visual.ShowPulse(new Color(0.9f, 0.55f, 1f, 0.8f), 0.9f, steps[nextIndex].warningDuration);
            }
            if (now < readyAt) return;
            int index = nextIndex++;
            pending = false;
            PhaseIndex = index;
            if (visual != null && steps[index].sprite != null) visual.SetPhaseSprite(steps[index].sprite);
            ability.ExecutePhase(index);
        }
    }
}
