using UnityEngine;

[RequireComponent(typeof(ChapterTwoLinkVisual), typeof(ChapterTwoActionAnimation))]
public sealed class PlateletRepairBehaviour : TowerAttackBehaviour
{
    [SerializeField, Min(.01f)] private float repairAmount = 25f;
    private TowerController tower;
    private OpenFieldPrototypeController owner;
    private ChapterTwoLinkVisual visual;
    private ChapterTwoActionAnimation action;
    private float cooldown, visibleUntil;
    public TowerCombatState Target { get; private set; }
    public override bool RequiresDamage => false;
    public override bool CanAttack => false;
    public override void Initialize(TowerDefinition definition, LayerMask enemyLayers)
    {
        EnemyRangeQuery.RequirePositive(repairAmount, nameof(repairAmount));
        tower = GetComponent<TowerController>();owner = FindObjectOfType<OpenFieldPrototypeController>();
        visual = GetComponent<ChapterTwoLinkVisual>();action = GetComponent<ChapterTwoActionAnimation>();
        cooldown = 0f;Target = null;action?.Stop();
    }
    private void Update()
    {
        if (owner == null || tower == null || tower.Definition == null || !owner.HasActiveRun) { visual?.Hide();return; }
        if (owner.IsPaused || Time.deltaTime <= 0f) return;
        cooldown -= Time.deltaTime;
        if (cooldown <= 0f)
        {
            Target = null;
            foreach (TowerCombatState wall in owner.Defenders)
                if (wall.IsAlive && wall.RepairBudgetRemaining > 0f && wall.CurrentHealth < wall.MaxHealth &&
                    (wall.transform.position - transform.position).sqrMagnitude <= tower.Definition.AttackRange * tower.Definition.AttackRange &&
                    (Target == null || wall.CurrentHealth / wall.MaxHealth < Target.CurrentHealth / Target.MaxHealth)) Target = wall;
            if (Target != null && Target.Repair(repairAmount) > 0f)
            {
                visibleUntil = Time.time + .35f;action?.PlayContact(Target.transform.position);
            }
            cooldown = tower.Definition.AttackInterval;
        }
        if (Target != null && Target.IsAlive && Time.time < visibleUntil)
            visual.Show(Target.transform.position, Target.RepairBudgetRemaining / (Target.MaxHealth * .6f), new Color(.55f, 1f, .65f));
        else visual.Hide();
        bool preparing = Target != null && Target.IsAlive && Target.RepairBudgetRemaining > 0f &&
            Target.CurrentHealth < Target.MaxHealth && cooldown < .18f &&
            (Target.transform.position - transform.position).sqrMagnitude <= tower.Definition.AttackRange * tower.Definition.AttackRange;
        if (preparing) action?.SetCharge(1f - cooldown / .18f);else action?.ClearCharge();
    }
    public override void Attack(EnemyController target, float damage, ProjectilePool pool) { }
    private void OnDisable() { Target = null;visual?.Hide();action?.Stop(); }
}
