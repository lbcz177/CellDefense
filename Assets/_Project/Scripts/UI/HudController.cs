using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.Serialization;

public class HudController : MonoBehaviour
{

    [SerializeField]
    private TextMeshProUGUI atpText;
    [SerializeField]
    private TextMeshProUGUI lifeText;
    [SerializeField]
    private TextMeshProUGUI stateText;
    [SerializeField]
    private Button pauseButton;
    [SerializeField]
    [FormerlySerializedAs("sellModeButton")]
    private Button sellButton;
    [SerializeField]
    [FormerlySerializedAs("upgradeModeButton")]
    private Button upgradeButton;
    [SerializeField]
    private GameObject resultPanel;
    [SerializeField]
    private TextMeshProUGUI resultText;
    [SerializeField]
    private Button restartButton;
    [SerializeField]
    private Button nextLevelButton;
    private Button guardTowerButton;
    private Button interceptorTowerButton;
    private Button phagocyteButton;
    private TextMeshProUGUI sellLabel;
    private TextMeshProUGUI upgradeLabel;
    [SerializeField, Min(0f)]
    [FormerlySerializedAs("towerActionButtonOffset")]
    private float slotActionButtonOffset = 65f;
    [SerializeField]
    private TowerDefinition guardTowerDefinition;
    [SerializeField]
    private TowerDefinition interceptorTowerDefinition;
    [SerializeField]
    private TowerDefinition phagocyteDefinition;

    private EconomyService economyService;
    private LifeService lifeService;
    private GameFlowController gameFlowController;
    private WaveController waveController;
    private BuildController buildController;
    private Canvas hudCanvas;
    private RectTransform hudRectTransform;
    private Camera gameplayCamera;
    private bool isSubscribed;

    public void Initialize(
        EconomyService economy,
        LifeService life,
        GameFlowController gameFlow,
        WaveController waves,
        BuildController build)
    {
        if (economy == null)
        {
            throw new ArgumentNullException(nameof(economy));
        }
        if (life == null)
        {
            throw new ArgumentNullException(nameof(life));
        }
        if (gameFlow == null)
        {
            throw new ArgumentNullException(nameof(gameFlow));
        }
        if (waves == null)
        {
            throw new ArgumentNullException(nameof(waves));
        }
        if (build == null)
        {
            throw new ArgumentNullException(nameof(build));
        }
        if (atpText == null)
        {
            throw new InvalidOperationException("ATP Text reference is not set in the inspector.");
        }
        if (lifeText == null)
        {
            throw new InvalidOperationException("Life Text reference is not set in the inspector.");
        }
        if (stateText == null)
        {
            throw new InvalidOperationException("State Text reference is not set in the inspector.");
        }
        if (pauseButton == null)
        {
            throw new InvalidOperationException("Pause Button reference is not set in the inspector.");
        }
        if (sellButton == null)
        {
            throw new InvalidOperationException("Sell Button reference is not set in the inspector.");
        }
        if (upgradeButton == null)
        {
            throw new InvalidOperationException("Upgrade Button reference is not set in the inspector.");
        }
        if (resultPanel == null)
        {
            throw new InvalidOperationException("Result Panel reference is not set in the inspector.");
        }
        if (resultText == null)
        {
            throw new InvalidOperationException("Result Text reference is not set in the inspector.");
        }
        if (restartButton == null)
        {
            throw new InvalidOperationException("Restart Button reference is not set in the inspector.");
        }
        if (guardTowerDefinition == null || interceptorTowerDefinition == null)
        {
            throw new InvalidOperationException("Tower definition references are not set in the inspector.");
        }
        if (guardTowerDefinition.PlacementKind != BuildPlacementKind.TowerSite ||
            interceptorTowerDefinition.PlacementKind != BuildPlacementKind.TowerSite)
        {
            throw new InvalidOperationException("Guard and interceptor definitions must use Tower Site placement.");
        }
        if (phagocyteDefinition != null && phagocyteDefinition.PlacementKind != BuildPlacementKind.RoadSite)
        {
            throw new InvalidOperationException("Phagocyte definition must use Road Site placement.");
        }

        economyService = economy;
        lifeService = life;
        gameFlowController = gameFlow;
        waveController = waves;
        buildController = build;
        hudCanvas = GetComponent<Canvas>();
        hudRectTransform = (RectTransform)transform;
        gameplayCamera = Camera.main;
        if (hudCanvas == null || gameplayCamera == null)
        {
            throw new InvalidOperationException("HUD Canvas and Main Camera are required for slot actions.");
        }
        sellLabel = sellButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (sellLabel == null)
        {
            throw new InvalidOperationException("Sell Button must have a TextMeshPro label.");
        }
        upgradeLabel = upgradeButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (upgradeLabel == null)
        {
            throw new InvalidOperationException("Upgrade Button must have a TextMeshPro label.");
        }
        if (sellButton.transform.parent != transform || upgradeButton.transform.parent != transform)
        {
            throw new InvalidOperationException("Tower action buttons must be direct children of the HUD Canvas.");
        }
        sellButton.gameObject.SetActive(false);
        upgradeButton.gameObject.SetActive(false);
        CreateTowerSelectionButtons();

        RefreshAll();
        if (isActiveAndEnabled)
        {
            Subscribe();
        }
    }

    private void OnEnable()
    {
        Subscribe();

        RefreshAll();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void LateUpdate()
    {
        if (buildController == null)
        {
            return;
        }
        BuildSlot slot = buildController.SelectedSlot;
        if (slot == null)
        {
            return;
        }
        if (slot.CurrentTower == null && guardTowerButton.gameObject.activeSelf)
        {
            PositionContextButton(guardTowerButton, slot.transform.position, slotActionButtonOffset);
            PositionContextButton(interceptorTowerButton, slot.transform.position, -slotActionButtonOffset);
        }
        else if (slot.CurrentTower == null && phagocyteButton != null && phagocyteButton.gameObject.activeSelf)
        {
            PositionContextButton(phagocyteButton, slot.transform.position, slotActionButtonOffset);
        }
        else if (slot.CurrentTower != null && sellButton.gameObject.activeSelf)
        {
            Vector3 towerPosition = slot.CurrentTower.transform.position;
            PositionContextButton(upgradeButton, towerPosition, slotActionButtonOffset);
            PositionContextButton(sellButton, towerPosition, -slotActionButtonOffset);
        }
    }

    private void RefreshATP(int currentATP)
    {
        if (atpText == null)
        {
            throw new InvalidOperationException("ATP Text reference is not set in the inspector.");
        }
        atpText.text = $"ATP: {currentATP}";
        if (buildController != null)
        {
            RefreshSlotActions(buildController.SelectedSlot);
        }
    }

    private void RefreshLife(int currentLife)
    {
        lifeText.text = $"生命：{currentLife}";
    }

    private void RefreshState(GameState state)
    {
        string stateLabel = state switch
        {
            GameState.Boot => "状态：启动中",
            GameState.Ready => "状态：准备中",
            GameState.Running => "状态：进行中",
            GameState.Paused => "状态：已暂停",
            GameState.Victory => "状态：胜利",
            GameState.Defeat => "状态：失败",
            _ => $"状态：{state}"
        };

        string waveLabel = waveController != null && waveController.TotalWaveCount > 0
            ? $"  波次：{waveController.CurrentWaveNumber}/{waveController.TotalWaveCount}"
            : string.Empty;
        stateText.text = stateLabel + waveLabel;

        pauseButton.interactable =
            state == GameState.Running || state == GameState.Paused;

        RefreshSlotActions(buildController.SelectedSlot);

        bool hasResult = state == GameState.Victory || state == GameState.Defeat;
        resultPanel.SetActive(hasResult);
        if (nextLevelButton != null)
        {
            nextLevelButton.gameObject.SetActive(state == GameState.Victory && gameFlowController.HasNextLevel);
        }
        if (hasResult)
        {
            resultText.text = state == GameState.Victory ? "胜利" : "失败";
        }
    }

    private void RefreshAll()
    {
        if (economyService == null || lifeService == null || gameFlowController == null)
        {
            return;
        }

        RefreshATP(economyService.CurrentATP);
        RefreshLife(lifeService.CurrentLife);
        RefreshState(gameFlowController.CurrentState);
    }

    private void HandlePauseClicked()
    {
        var currentState = gameFlowController.CurrentState;
        if (currentState == GameState.Running)
        {
            gameFlowController.Pause();
        }
        else if (currentState == GameState.Paused)
        {
            gameFlowController.Resume();
        }
    }

    private void HandleRestartClicked()
    {
        gameFlowController.RestartRun();
    }

    private void HandleNextLevelClicked()
    {
        gameFlowController.TryLoadNextLevel();
    }

    private void HandleWaveChanged(int currentWave, int totalWaves)
    {
        RefreshState(gameFlowController.CurrentState);
    }

    private void HandleGuardTowerClicked()
    {
        BuildSlot slot = buildController.SelectedSlot;
        if (slot != null)
        {
            buildController.TryBuild(slot, guardTowerDefinition);
        }
    }

    private void HandleInterceptorTowerClicked()
    {
        BuildSlot slot = buildController.SelectedSlot;
        if (slot != null)
        {
            buildController.TryBuild(slot, interceptorTowerDefinition);
        }
    }

    private void HandlePhagocyteClicked()
    {
        BuildSlot slot = buildController.SelectedSlot;
        if (slot != null && phagocyteDefinition != null)
        {
            buildController.TryBuild(slot, phagocyteDefinition);
        }
    }

    private void HandleSellClicked()
    {
        BuildSlot slot = buildController.SelectedSlot;
        if (slot != null)
        {
            buildController.TrySell(slot);
        }
    }

    private void HandleUpgradeClicked()
    {
        BuildSlot slot = buildController.SelectedSlot;
        if (slot != null)
        {
            buildController.TryUpgrade(slot);
            RefreshSlotActions(buildController.SelectedSlot);
        }
    }

    private void RefreshSlotActions(BuildSlot slot)
    {
        bool running = gameFlowController.CurrentState == GameState.Running;
        bool showBuild = running && slot != null && slot.CurrentTower == null;
        bool showTower = running && slot != null && slot.CurrentTower != null;
        bool showTowerBuild = showBuild && slot.PlacementKind == BuildPlacementKind.TowerSite;
        bool showRoadBuild = showBuild && slot.PlacementKind == BuildPlacementKind.RoadSite &&
                             phagocyteButton != null;
        guardTowerButton.gameObject.SetActive(showTowerBuild);
        interceptorTowerButton.gameObject.SetActive(showTowerBuild);
        if (phagocyteButton != null)
        {
            phagocyteButton.gameObject.SetActive(showRoadBuild);
        }
        sellButton.gameObject.SetActive(showTower);
        upgradeButton.gameObject.SetActive(showTower);

        if (showTowerBuild)
        {
            guardTowerButton.interactable = economyService.CurrentATP >= guardTowerDefinition.BuildCost;
            interceptorTowerButton.interactable = economyService.CurrentATP >= interceptorTowerDefinition.BuildCost;
            PositionContextButton(guardTowerButton, slot.transform.position, slotActionButtonOffset);
            PositionContextButton(interceptorTowerButton, slot.transform.position, -slotActionButtonOffset);
            return;
        }
        if (showRoadBuild)
        {
            phagocyteButton.interactable = economyService.CurrentATP >= phagocyteDefinition.BuildCost;
            PositionContextButton(phagocyteButton, slot.transform.position, slotActionButtonOffset);
            return;
        }
        if (!showTower)
        {
            return;
        }

        TowerController tower = slot.CurrentTower;
        sellLabel.text = $"出售 +{buildController.GetSellRefund(slot)}";
        upgradeLabel.text = tower.IsUpgraded ? "已升级" : $"升级 {tower.BuildCostPaid}";
        upgradeButton.interactable = !tower.IsUpgraded &&
                                     economyService.CurrentATP >= tower.BuildCostPaid;
        PositionContextButton(upgradeButton, tower.transform.position, slotActionButtonOffset);
        PositionContextButton(sellButton, tower.transform.position, -slotActionButtonOffset);
    }

    private void PositionContextButton(Button button, Vector3 worldPosition, float verticalOffset)
    {
        Camera uiCamera = hudCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : hudCanvas.worldCamera;
        Vector2 screenPosition = gameplayCamera.WorldToScreenPoint(worldPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            hudRectTransform, screenPosition, uiCamera, out Vector2 localPosition);

        RectTransform buttonRect = button.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = localPosition + new Vector2(0f, verticalOffset);
    }

    private void CreateTowerSelectionButtons()
    {
        if (guardTowerButton != null || interceptorTowerButton != null || phagocyteButton != null)
        {
            return;
        }

        guardTowerButton = CreateTowerSelectionButton(guardTowerDefinition, new Vector2(20f, 20f));
        interceptorTowerButton = CreateTowerSelectionButton(interceptorTowerDefinition, new Vector2(200f, 20f));
        guardTowerButton.gameObject.SetActive(false);
        interceptorTowerButton.gameObject.SetActive(false);
        if (phagocyteDefinition != null)
        {
            phagocyteButton = CreateTowerSelectionButton(phagocyteDefinition, new Vector2(380f, 20f));
            phagocyteButton.gameObject.SetActive(false);
        }
    }

    private Button CreateTowerSelectionButton(TowerDefinition definition, Vector2 anchoredPosition)
    {
        Button button = Instantiate(pauseButton, pauseButton.transform.parent);
        button.name = $"{definition.DisplayName} Button";

        RectTransform rectTransform = button.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
        rectTransform.pivot = Vector2.zero;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(170f, 40f);

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        label.text = $"{definition.DisplayName}  {definition.BuildCost} ATP";
        label.fontSize = 22f;

        return button;
    }

    private void Subscribe()
    {
        if (isSubscribed || economyService == null || lifeService == null ||
            gameFlowController == null || waveController == null || buildController == null ||
            pauseButton == null || sellButton == null || upgradeButton == null ||
            guardTowerButton == null || interceptorTowerButton == null)
        {
            return;
        }

        economyService.ATPChanged += RefreshATP;
        lifeService.LifeChanged += RefreshLife;
        gameFlowController.StateChanged += RefreshState;
        waveController.WaveChanged += HandleWaveChanged;
        buildController.SelectedSlotChanged += RefreshSlotActions;
        pauseButton.onClick.AddListener(HandlePauseClicked);
        sellButton.onClick.AddListener(HandleSellClicked);
        upgradeButton.onClick.AddListener(HandleUpgradeClicked);
        restartButton.onClick.AddListener(HandleRestartClicked);
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.AddListener(HandleNextLevelClicked);
        }
        guardTowerButton.onClick.AddListener(HandleGuardTowerClicked);
        interceptorTowerButton.onClick.AddListener(HandleInterceptorTowerClicked);
        if (phagocyteButton != null)
        {
            phagocyteButton.onClick.AddListener(HandlePhagocyteClicked);
        }
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed)
        {
            return;
        }

        economyService.ATPChanged -= RefreshATP;
        lifeService.LifeChanged -= RefreshLife;
        gameFlowController.StateChanged -= RefreshState;
        waveController.WaveChanged -= HandleWaveChanged;
        buildController.SelectedSlotChanged -= RefreshSlotActions;
        pauseButton.onClick.RemoveListener(HandlePauseClicked);
        sellButton.onClick.RemoveListener(HandleSellClicked);
        upgradeButton.onClick.RemoveListener(HandleUpgradeClicked);
        restartButton.onClick.RemoveListener(HandleRestartClicked);
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveListener(HandleNextLevelClicked);
        }
        guardTowerButton.onClick.RemoveListener(HandleGuardTowerClicked);
        interceptorTowerButton.onClick.RemoveListener(HandleInterceptorTowerClicked);
        if (phagocyteButton != null)
        {
            phagocyteButton.onClick.RemoveListener(HandlePhagocyteClicked);
        }
        isSubscribed = false;
    }
}
