using UnityEngine;

[CreateAssetMenu(fileName = "TowerDefinition", menuName = "Cell Defense/Tower Definition")]
public class TowerDefinition : ScriptableObject
{
    [SerializeField]
    private TowerController prefab;
    public TowerController Prefab => prefab;
    [SerializeField, Min(1)]
    private int buildCost = 25;
    public int BuildCost => buildCost;
    [SerializeField, Min(0.01f)]
    private float attackRange = 2.5f;
    [SerializeField, Min(0.01f)]
    private float damage = 25f;
    [SerializeField, Min(0.01f)]
    private float attackInterval = 1f;
    public float AttackRange => attackRange;
    public float AttackInterval => attackInterval;
    public float Damage => damage;
}
