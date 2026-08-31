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
}
