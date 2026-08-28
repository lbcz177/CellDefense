using UnityEngine;

public class BuildSlot : MonoBehaviour
{
    public bool IsOccupied { get; private set; }

    public bool CanBuild()
    {
        // TODO: Report whether this slot currently accepts a tower.
        return false;
    }

    public void Occupy(TowerController tower)
    {
        // TODO: Record the tower that owns this slot.
    }

    public void Release()
    {
        // TODO: Clear the tower reference and make the slot available again.
    }
}
