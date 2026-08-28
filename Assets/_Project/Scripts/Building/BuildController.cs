using UnityEngine;

public class BuildController : MonoBehaviour
{
    public bool TryBuild(BuildSlot slot, TowerDefinition definition)
    {
        // TODO: Validate, pay, create, initialize, and occupy as one operation.
        return false;
    }

    public bool TrySell(BuildSlot slot)
    {
        // TODO: Refund, remove the tower, and release its slot as one operation.
        return false;
    }
}
