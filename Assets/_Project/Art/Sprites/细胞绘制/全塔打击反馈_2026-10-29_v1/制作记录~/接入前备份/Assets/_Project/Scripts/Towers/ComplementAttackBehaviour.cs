using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ChapterTwoLinkVisual), typeof(ChapterTwoActionAnimation))]
public sealed class ComplementAttackBehaviour : TowerAttackBehaviour
{
    [SerializeField, Min(.01f)] private float assemblySeconds = 4f;
    [SerializeField, Min(1f)] private float markedSpeedMultiplier = 2f;
    private TowerController tower;
    private OpenFieldPrototypeController owner;
    private ChapterTwoLinkVisual visual;
    private ChapterTwoActionAnimation action;
    private ContactFilter2D filter;
    private readonly List<Collider2D> candidates = new List<Collider2D>();
    private uint version;
    private float cooldown;
    public EnemyController Target { get; private set; }
    public float AssemblyProgress { get; private set; }
    public override bool CanAttack => false;
    public override void Initialize(TowerDefinition definition, LayerMask enemyLayers)
    {
        EnemyRangeQuery.RequirePositive(assemblySeconds, nameof(assemblySeconds));
        EnemyRangeQuery.RequirePositive(markedSpeedMultiplier, nameof(markedSpeedMultiplier));
        tower = GetComponent<TowerController>();owner = FindObjectOfType<OpenFieldPrototypeController>();
        visual = GetComponent<ChapterTwoLinkVisual>();action = GetComponent<ChapterTwoActionAnimation>();action?.Stop();filter = new ContactFilter2D();
        filter.SetLayerMask(enemyLayers);filter.useTriggers = true;ResetTarget();cooldown = 0f;
    }
    private bool Valid(EnemyController enemy) => enemy != null && enemy.isActiveAndEnabled && enemy.CanBeTargeted &&
        (enemy.transform.position - transform.position).sqrMagnitude <= tower.Definition.AttackRange * tower.Definition.AttackRange;
    private void ResetTarget() { Target = null;version = 0;AssemblyProgress = 0f;visual?.Hide();action?.ClearCharge(); }
    private void Update()
    {
        if (tower == null || tower.Definition == null || owner == null || !owner.HasActiveRun) { ResetTarget();return; }
        if (owner.IsPaused || Time.deltaTime <= 0f) return;
        if (!Valid(Target) || Target.SpawnVersion != version) ResetTarget();
        if (cooldown > 0f) { cooldown -= Time.deltaTime;return; }
        if (Target == null)
        {
            Physics2D.OverlapCircle(transform.position, tower.Definition.AttackRange, filter, candidates);
            foreach (Collider2D collider in candidates)
            {
                EnemyController enemy = collider.GetComponentInParent<EnemyController>();
                if (Valid(enemy) && (Target == null || enemy.RemainingPathDistance < Target.RemainingPathDistance)) Target = enemy;
            }
            if (Target == null) return;
            version = Target.SpawnVersion;
        }
        EnemyImmuneState immune = Target.GetComponent<EnemyImmuneState>();
        bool marked = immune != null && immune.IsMarked;
        AssemblyProgress = Mathf.Min(1f, AssemblyProgress + Time.deltaTime * (marked ? markedSpeedMultiplier : 1f) / assemblySeconds);
        action?.SetCharge(AssemblyProgress);
        visual.Show(Target.transform.position, AssemblyProgress, marked ? new Color(1f, .85f, .35f) : new Color(.35f, .9f, 1f));
        if (AssemblyProgress < 1f) return;
        EnemyController completed = Target;
        float damage = tower.CurrentDamage * (completed.Definition.ComplementSensitive ? 1.5f : 1f);
        action?.PlayContact(completed.transform.position);
        ResetTarget();cooldown = tower.Definition.AttackInterval;
        completed.TakeDamage(damage); // Uses the existing armor -> mark -> shield pipeline, including Bosses.
    }
    public override void Attack(EnemyController target, float damage, ProjectilePool pool) { }
    private void OnDisable() { ResetTarget();action?.Stop(); }
}
