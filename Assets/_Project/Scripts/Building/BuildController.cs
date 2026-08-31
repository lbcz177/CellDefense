using UnityEngine;
using System;

public class BuildController : MonoBehaviour
{
    [SerializeField]
    private BuildSlot[] slots;
    [SerializeField]
    private TowerDefinition selectedDefinition;
    private EconomyService economyService;

    public void Initialize(EconomyService economyService)
    {
        if(economyService == null)
        {
            throw new ArgumentNullException(nameof(economyService));
        }
        this.economyService = economyService;
    }
    public bool TryBuild(BuildSlot slot, TowerDefinition definition)
    {
        if(slot == null)
        {
            throw new ArgumentNullException(nameof(slot));
        }
        if(definition == null)
        {
            throw new ArgumentNullException(nameof(definition));
        }
        if(economyService == null)
        {
            throw new InvalidOperationException("EconomyService is not initialized.");
        }
        if(slot.CanBuild() == false)
        {
            return false;
        }
        if(definition.Prefab == null)
        {
            throw new InvalidOperationException("Cannot build a tower without a prefab.");
        }
        int buildCost = definition.BuildCost;
        if(buildCost <= 0)
        {
            throw new InvalidOperationException("Tower build cost must be greater than zero.");
        }
        if(!economyService.TrySpend(buildCost))
        {
            return false;
        }
        TowerController tower = null;
        try
        {
            tower = Instantiate(definition.Prefab, slot.transform.position, Quaternion.identity);
            tower.Initialize(definition);
            slot.Occupy(tower);
            return true;
        }
        catch
        {
            if(tower != null)
            {
                Destroy(tower.gameObject);
            }
            economyService.AddATP(buildCost);
            throw;
        }
        
    }

    private void HandleSlotClicked(BuildSlot slot)
    {
        if(slot == null)
        {
            throw new ArgumentNullException(nameof(slot));
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
