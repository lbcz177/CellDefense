using Unity.VisualScripting;
using UnityEngine;

public class BuildController : MonoBehaviour
{
    [SerializeField]
    private BuildSlot[] slots;
    [SerializeField]
    private TowerDefinition selectedDefinition;

    public bool TryBuild(BuildSlot slot, TowerDefinition definition)
    {
        if(slot == null)
        {
            throw new System.ArgumentNullException(nameof(slot));
        }
        if(definition == null)
        {
            throw new System.ArgumentNullException(nameof(definition));
        }
        if(slot.CanBuild() == false)
        {
            return false;
        }
        if(definition.Prefab == null)
        {
            throw new System.InvalidOperationException("Cannot build a tower without a prefab.");
        }
        TowerController tower = Instantiate(definition.Prefab, slot.transform.position, Quaternion.identity);
        tower.Initialize(definition);
        slot.Occupy(tower);
        return true;
    }

    private void HandleSlotClicked(BuildSlot slot)
    {
        if(slot == null)
        {
            throw new System.ArgumentNullException(nameof(slot));
        }
        if(selectedDefinition == null)
        {
            Debug.LogWarning("No tower definition selected.");
            return;
        }
        TryBuild(slot, selectedDefinition);
    }
    public bool TrySell(BuildSlot slot)
    {
        // TODO: Refund, remove the tower, and release its slot as one operation.
        return false;
    }

    void OnEnable()
    {
        foreach (var slot in slots)
        {
            slot.Clicked += HandleSlotClicked;
        }
    }

    void OnDisable()
    {
        foreach (var slot in slots)
        {
            slot.Clicked -= HandleSlotClicked;
        }
    }
}
