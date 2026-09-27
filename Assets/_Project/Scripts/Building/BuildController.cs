using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class BuildController : MonoBehaviour
{
    [SerializeField]
    private BuildSlot[] slots;
    [SerializeField, Range(0, 100)]
    private int sellRefundPercent = 50;
    private EconomyService economyService;
    private GameFlowController gameFlowController;
    private ProjectilePool projectilePool;
    private BuildSlot selectedSlot;

    public event Action<BuildSlot> SelectedSlotChanged;
    public BuildSlot SelectedSlot => selectedSlot;
    public int SellRefundPercent => sellRefundPercent;

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
        if (sellRefundPercent < 0 || sellRefundPercent > 100)
        {
            throw new InvalidOperationException("Sell refund percent must be between 0 and 100.");
        }
        if (this.gameFlowController != null)
        {
            this.gameFlowController.StateChanged -= HandleGameStateChanged;
        }
        this.economyService = economyService;
        this.gameFlowController = gameFlowController;
        this.projectilePool = projectilePool;
        this.gameFlowController.StateChanged += HandleGameStateChanged;
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

        if (selectedSlot == slot)
        {
            SelectSlot(null);
        }
        return true;
    }

    private void HandleSlotClicked(BuildSlot slot)
    {
        if(slot == null)
        {
            throw new ArgumentNullException(nameof(slot));
        }
        if (gameFlowController == null || gameFlowController.CurrentState != GameState.Running)
        {
            return;
        }
        SelectSlot(selectedSlot == slot ? null : slot);
    }

    private void LateUpdate()
    {
        if (selectedSlot == null || gameFlowController == null ||
            gameFlowController.CurrentState != GameState.Running || !Input.GetMouseButtonDown(0))
        {
            return;
        }
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Camera gameplayCamera = Camera.main;
        if (gameplayCamera == null)
        {
            return;
        }
        Vector3 screenPosition = Input.mousePosition;
        screenPosition.z = selectedSlot.transform.position.z - gameplayCamera.transform.position.z;
        Vector2 worldPosition = gameplayCamera.ScreenToWorldPoint(screenPosition);
        foreach (BuildSlot slot in slots)
        {
            if (slot == null)
            {
                continue;
            }
            Collider2D slotCollider = slot.GetComponent<Collider2D>();
            if (slotCollider != null && slotCollider.OverlapPoint(worldPosition))
            {
                return;
            }
        }

        SelectSlot(null);
    }

    private void SelectSlot(BuildSlot slot)
    {
        if (selectedSlot == slot)
        {
            return;
        }
        selectedSlot = slot;
        SelectedSlotChanged?.Invoke(selectedSlot);
    }

    public int GetSellRefund(BuildSlot slot)
    {
        if (slot == null || slot.CurrentTower == null)
        {
            return 0;
        }
        TowerController tower = slot.CurrentTower;
        long totalSpent = (long)tower.BuildCostPaid + tower.UpgradeCostPaid;
        return (int)(totalSpent * sellRefundPercent / 100);
    }

    public bool TrySell(BuildSlot slot)
    {
        if (slot == null)
        {
            throw new ArgumentNullException(nameof(slot));
        }
        if (economyService == null || gameFlowController == null)
        {
            throw new InvalidOperationException("BuildController is not initialized.");
        }
        if (gameFlowController.CurrentState != GameState.Running)
        {
            return false;
        }

        TowerController tower = slot.CurrentTower;
        if (tower == null)
        {
            return false;
        }
        if (tower.Definition == null || tower.BuildCostPaid <= 0)
        {
            throw new InvalidOperationException("Cannot sell a tower without a valid build cost.");
        }

        int refund = GetSellRefund(slot);
        tower.gameObject.SetActive(false);
        slot.Release();
        Destroy(tower.gameObject);
        if (selectedSlot == slot)
        {
            SelectSlot(null);
        }
        if (refund > 0)
        {
            economyService.AddATP(refund);
        }
        return true;
    }

    public bool TryUpgrade(BuildSlot slot)
    {
        if (slot == null)
        {
            throw new ArgumentNullException(nameof(slot));
        }
        if (economyService == null || gameFlowController == null)
        {
            throw new InvalidOperationException("BuildController is not initialized.");
        }
        if (gameFlowController.CurrentState != GameState.Running)
        {
            return false;
        }

        TowerController tower = slot.CurrentTower;
        if (tower == null || tower.IsUpgraded)
        {
            return false;
        }
        int upgradeCost = tower.BuildCostPaid;
        if (upgradeCost <= 0)
        {
            throw new InvalidOperationException("Cannot upgrade a tower without a valid build cost.");
        }
        if (!economyService.TrySpend(upgradeCost))
        {
            return false;
        }
        if (!tower.TryUpgrade(upgradeCost))
        {
            economyService.AddATP(upgradeCost);
            return false;
        }

        return true;
    }

    private void HandleGameStateChanged(GameState state)
    {
        if (state != GameState.Running)
        {
            SelectSlot(null);
        }
    }

    private void OnDestroy()
    {
        if (gameFlowController != null)
        {
            gameFlowController.StateChanged -= HandleGameStateChanged;
        }
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
