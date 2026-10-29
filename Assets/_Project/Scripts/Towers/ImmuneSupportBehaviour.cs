using UnityEngine;

// A passive support strategy; the regional system resolves targets and nonstacking effects.
[RequireComponent(typeof(ChapterTwoLinkVisual))]
public sealed class ImmuneSupportBehaviour : TowerAttackBehaviour
{
    [SerializeField] private bool regulatory;
    public bool IsRegulatory => regulatory;
    public AntibodyAttackBehaviour SupportedAntibody { get; internal set; }
    public int SupportedRegion { get; internal set; } = -1;
    public override bool RequiresDamage => false;
    public override bool CanAttack => false;
    public override void Attack(EnemyController target,float damage,ProjectilePool pool) { }
    private void OnDisable()
    {
        SupportedAntibody=null;SupportedRegion=-1;
        var visual=GetComponent<ChapterTwoLinkVisual>();if(visual!=null)visual.Hide();
    }
}
