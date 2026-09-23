using UnityEngine;
using System;

public class BuildController : MonoBehaviour
{
    [SerializeField]
    private BuildSlot[] slots;
    [SerializeField]
    private TowerDefinition selectedDefinition;
    private EconomyService economyService;
    private GameFlowController gameFlowController;
    private ProjectilePool projectilePool;

    public event Action<TowerDefinition> SelectedDefinitionChanged;
    public TowerDefinition SelectedDefinition => selectedDefinition;

    public void Initialize(
        EconomyService economyService,
        GameFlowController gameFlowController,
        ProjectilePool projectilePool)
    {
        if(economyService == null)
        {
            throw new ArgumentNullException(nameof(economyService));
        }
        if(gameFlowController == null)
        {
            throw new ArgumentNullException(nameof(gameFlowController));
        }
        if (projectilePool == null)
        {
            throw new ArgumentNullException(nameof(projectilePool));
        }
        this.economyService = economyService;
        this.gameFlowController = gameFlowController;
        this.projectilePool = projectilePool;
    }

    public bool TrySelectDefinition(TowerDefinition definition)
    {
        if (definition == null)
        {
            throw new ArgumentNullException(nameof(definition));
        }
        if (gameFlowController == null)
        {
            throw new InvalidOperationException("GameFlowController is not initialized.");
        }
        GameState currentState = gameFlowController.CurrentState;
        bool canSelect = currentState == GameState.Running || currentState == GameState.Paused || currentState == GameState.Ready;
        if (!canSelect)
        {
            Debug.LogWarning($"Cannot select tower definition in the current game state: {currentState}");
            return false;
        }
        if (selectedDefinition == definition)
        {
            return false;
        }
        selectedDefinition = definition;
        SelectedDefinitionChanged?.Invoke(selectedDefinition);
        return true;
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
        if(gameFlowController == null)
        {
            throw new InvalidOperationException("GameFlowController is not initialized.");
        }
        if(gameFlowController.CurrentState != GameState.Running)
        {
            return false;
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
            tower.Initialize(definition, projectilePool);
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
