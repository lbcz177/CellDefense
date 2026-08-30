using UnityEngine;

public class TowerController : MonoBehaviour
{
    public TowerDefinition Definition{ get; private set; }
    public void Initialize(TowerDefinition definition)
    {
        if(definition == null)
        {
            throw new System.ArgumentNullException(nameof(definition));
        }
        Definition = definition;
    }

    private void Update()
    {
        // TODO: Advance attack timing only while gameplay is running.
    }

    private EnemyController FindTarget()
    {
        // TODO: Select one valid enemy according to the current target rule.
        return null;
    }

    private void Attack(EnemyController target)
    {
        // TODO: Create one attack request against the selected target.
    }
}
