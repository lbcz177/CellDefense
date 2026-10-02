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
    [SerializeField]
    private Button returnToMenuButton;
    [SerializeField]
    private GameObject victoryPanel;
    [SerializeField]
    private GameObject defeatPanel;
    [SerializeField]
    private Button victoryNextLevelButton;
    private bool createdReturnToMenuButton;
    private bool useCustomResultPanels;
    private Button guardTowerButton;
    private Button interceptorTowerButton;
    private Button phagocyteButton;
    private TextMeshProUGUI sellLabel;
    private TextMeshProUGUI upgradeLabel;
    private RectTransform towerInfoPanel;
    private TextMeshProUGUI towerInfoText;
    private GameObject sellConfirmation;
    private TextMeshProUGUI sellConfirmationText;
    private Button sellConfirmationBackdrop;
    private Button confirmSellButton;
    private Button cancelSellButton;
    private BuildSlot pendingSellSlot;
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
    private TowerBuildPreview towerBuildPreview;
    private TowerDefinition hoveredBuildDefinition;
    private BuildSlot previewSlot;
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
        if ((victoryPanel == null) != (defeatPanel == null))
        {
            throw new InvalidOperationException("Victory and Defeat Panels must both be assigned in the inspector.");
        }
        useCustomResultPanels = victoryPanel != null;
        if (!useCustomResultPanels && (resultPanel == null || resultText == null || restartButton == null))
        {
            throw new InvalidOperationException("Legacy result panel, text and restart button references are required when custom result panels are not assigned.");
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
        if (!useCustomResultPanels)
        {
            CreateReturnToMenuButtonIfNeeded();
        }
        sellButton.gameObject.SetActive(false);
        upgradeButton.gameObject.SetActive(false);
        CreateTowerSelectionButtons();
        CreateTowerInfoPanel();
        CreateSellConfirmation();
        towerBuildPreview = gameObject.AddComponent<TowerBuildPreview>();

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
        ClearBuildPreview();
        CloseSellConfirmation();
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
            PositionBuildChoices(slot, false);
        }
        else if (slot.CurrentTower == null && phagocyteButton != null && phagocyteButton.gameObject.activeSelf)
        {
            PositionBuildChoices(slot, true);
        }
        else if (slot.CurrentTower != null && sellButton.gameObject.activeSelf)
        {
            Vector3 towerPosition = slot.CurrentTower.transform.position;
            PositionContextButton(upgradeButton, towerPosition, slotActionButtonOffset);
            PositionContextButton(sellButton, towerPosition, -slotActionButtonOffset);
            PositionTowerInfoPanel(towerPosition);
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
        if (useCustomResultPanels)
        {
            if (resultPanel != null)
            {
                resultPanel.SetActive(false);
            }
            victoryPanel.SetActive(state == GameState.Victory);
            defeatPanel.SetActive(state == GameState.Defeat);
            if (victoryNextLevelButton != null)
            {
                victoryNextLevelButton.gameObject.SetActive(gameFlowController.HasNextLevel);
            }
            return;
        }

        resultPanel.SetActive(hasResult);
        if (nextLevelButton != null)
        {
            nextLevelButton.gameObject.SetActive(state == GameState.Victory && gameFlowController.HasNextLevel);
        }
        if (returnToMenuButton != null)
        {
            returnToMenuButton.interactable = hasResult;
            if (createdReturnToMenuButton && hasResult)
            {
                PositionReturnToMenuButton(state == GameState.Victory && gameFlowController.HasNextLevel);
            }
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

    public void RestartLevel()
    {
        gameFlowController?.RestartRun();
    }

    public void LoadNextLevel()
    {
        gameFlowController?.TryLoadNextLevel();
    }

    public void ReturnToMainMenu()
    {
        gameFlowController?.TryReturnToMainMenu();
    }

    private void CreateReturnToMenuButtonIfNeeded()
    {
        if (returnToMenuButton != null)
        {
            return;
        }

        returnToMenuButton = Instantiate(restartButton, resultPanel.transform, false);
        returnToMenuButton.name = "Return To Main Menu Button";
        returnToMenuButton.onClick = new Button.ButtonClickedEvent();
        TextMeshProUGUI label = returnToMenuButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label == null)
        {
            throw new InvalidOperationException("Restart Button must have a TextMeshPro label.");
        }
        label.text = "返回主菜单";
        createdReturnToMenuButton = true;
        PositionReturnToMenuButton(nextLevelButton != null);
    }

    private void PositionReturnToMenuButton(bool showNextLevel)
    {
        Button previousButton = showNextLevel && nextLevelButton != null
            ? nextLevelButton : restartButton;
        RectTransform previousRect = previousButton.GetComponent<RectTransform>();
        RectTransform returnRect = returnToMenuButton.GetComponent<RectTransform>();
        returnRect.anchorMin = previousRect.anchorMin;
        returnRect.anchorMax = previousRect.anchorMax;
        returnRect.pivot = previousRect.pivot;
        returnRect.anchoredPosition = previousRect.anchoredPosition + new Vector2(0f, -40f);
        returnRect.sizeDelta = previousRect.sizeDelta;
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
        if (gameFlowController.CurrentState == GameState.Running &&
            slot != null && slot.CurrentTower != null)
        {
            pendingSellSlot = slot;
            RefreshSellConfirmationText();
            sellConfirmation.SetActive(true);
        }
    }

    private void HandleConfirmSellClicked()
    {
        BuildSlot slot = pendingSellSlot;
        CloseSellConfirmation();
        if (slot != null && buildController.SelectedSlot == slot)
        {
            buildController.TrySell(slot);
        }
    }

    private void CloseSellConfirmation()
    {
        pendingSellSlot = null;
        if (sellConfirmation != null)
        {
            sellConfirmation.SetActive(false);
        }
    }

    private void RefreshSellConfirmationText()
    {
        if (pendingSellSlot == null || pendingSellSlot.CurrentTower == null)
        {
            CloseSellConfirmation();
            return;
        }

        sellConfirmationText.text = $"出售 {pendingSellSlot.CurrentTower.Definition.DisplayName}？\n" +
                                    $"返还 {buildController.GetSellRefund(pendingSellSlot)} ATP";
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
        if (pendingSellSlot != null)
        {
            if (!showTower || slot != pendingSellSlot)
            {
                CloseSellConfirmation();
            }
            else
            {
                RefreshSellConfirmationText();
            }
        }
        if (!showBuild || slot != previewSlot)
        {
            ClearBuildPreview();
        }
        guardTowerButton.gameObject.SetActive(showTowerBuild);
        interceptorTowerButton.gameObject.SetActive(showTowerBuild);
        if (phagocyteButton != null)
        {
            phagocyteButton.gameObject.SetActive(showRoadBuild);
        }
        sellButton.gameObject.SetActive(showTower);
        upgradeButton.gameObject.SetActive(showTower);
        towerInfoPanel.gameObject.SetActive(showTower);

        if (showTowerBuild)
        {
            RefreshBuildChoice(guardTowerButton, guardTowerDefinition);
            RefreshBuildChoice(interceptorTowerButton, interceptorTowerDefinition);
            PositionBuildChoices(slot, false);
            RefreshVisibleBuildPreview();
            return;
        }
        if (showRoadBuild)
        {
            RefreshBuildChoice(phagocyteButton, phagocyteDefinition);
            PositionBuildChoices(slot, true);
            RefreshVisibleBuildPreview();
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
        string upgradeCost = tower.IsUpgraded ? "已满级" : $"{tower.BuildCostPaid} ATP";
        towerInfoText.text = $"{tower.Definition.DisplayName}  Lv.{(tower.IsUpgraded ? 2 : 1)}\n" +
                             $"射程  {tower.Definition.AttackRange:0.#}\n" +
                             $"伤害  {tower.CurrentDamage:0.#}\n" +
                             $"攻击间隔  {tower.Definition.AttackInterval:0.##} 秒\n" +
                             $"升级  {upgradeCost}\n" +
                             $"出售  +{buildController.GetSellRefund(slot)} ATP";
        PositionContextButton(upgradeButton, tower.transform.position, slotActionButtonOffset);
        PositionContextButton(sellButton, tower.transform.position, -slotActionButtonOffset);
        PositionTowerInfoPanel(tower.transform.position);
        towerBuildPreview.ShowBuiltTower(tower);
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

    private void PositionTowerInfoPanel(Vector3 worldPosition)
    {
        Camera uiCamera = hudCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : hudCanvas.worldCamera;
        Vector2 screenPosition = gameplayCamera.WorldToScreenPoint(worldPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            hudRectTransform, screenPosition, uiCamera, out Vector2 localPosition);

        Rect canvasRect = hudRectTransform.rect;
        float halfWidth = towerInfoPanel.rect.width * 0.5f;
        float halfHeight = towerInfoPanel.rect.height * 0.5f;
        float rightX = localPosition.x + 200f;
        float leftX = localPosition.x - 200f;
        float preferredX = rightX + halfWidth <= canvasRect.xMax - 8f ? rightX : leftX;
        float x = Mathf.Clamp(preferredX,
            canvasRect.xMin + halfWidth + 8f, canvasRect.xMax - halfWidth - 8f);
        float y = Mathf.Clamp(localPosition.y,
            canvasRect.yMin + halfHeight + 8f, canvasRect.yMax - halfHeight - 8f);

        towerInfoPanel.anchorMin = new Vector2(0.5f, 0.5f);
        towerInfoPanel.anchorMax = new Vector2(0.5f, 0.5f);
        towerInfoPanel.pivot = new Vector2(0.5f, 0.5f);
        towerInfoPanel.anchoredPosition = new Vector2(x, y);
    }

    private void CreateTowerInfoPanel()
    {
        GameObject panel = new GameObject("Tower Info Panel", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        panel.layer = gameObject.layer;
        panel.transform.SetParent(transform, false);
        towerInfoPanel = panel.GetComponent<RectTransform>();
        towerInfoPanel.sizeDelta = new Vector2(230f, 166f);

        Image background = panel.GetComponent<Image>();
        background.color = new Color(0.1f, 0.14f, 0.21f, 0.92f);
        background.raycastTarget = false;

        towerInfoText = Instantiate(sellLabel, panel.transform, false);
        towerInfoText.name = "Tower Stats";
        towerInfoText.fontSize = 18f;
        towerInfoText.enableAutoSizing = false;
        towerInfoText.alignment = TextAlignmentOptions.TopLeft;
        towerInfoText.color = Color.white;
        towerInfoText.raycastTarget = false;
        RectTransform textRect = towerInfoText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 8f);
        textRect.offsetMax = new Vector2(-8f, -8f);
        panel.SetActive(false);
    }

    private void CreateSellConfirmation()
    {
        sellConfirmation = new GameObject("Sell Confirmation", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(Button));
        sellConfirmation.layer = gameObject.layer;
        sellConfirmation.transform.SetParent(transform, false);
        RectTransform backdropRect = sellConfirmation.GetComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;
        Image backdrop = sellConfirmation.GetComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.55f);
        sellConfirmationBackdrop = sellConfirmation.GetComponent<Button>();
        sellConfirmationBackdrop.targetGraphic = backdrop;
        sellConfirmationBackdrop.transition = Selectable.Transition.None;

        GameObject dialog = new GameObject("Dialog", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        dialog.layer = gameObject.layer;
        dialog.transform.SetParent(sellConfirmation.transform, false);
        RectTransform dialogRect = dialog.GetComponent<RectTransform>();
        dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
        dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
        dialogRect.pivot = new Vector2(0.5f, 0.5f);
        dialogRect.anchoredPosition = Vector2.zero;
        dialogRect.sizeDelta = new Vector2(380f, 190f);
        dialog.GetComponent<Image>().color = new Color(0.1f, 0.14f, 0.21f, 0.98f);

        sellConfirmationText = Instantiate(sellLabel, dialog.transform, false);
        sellConfirmationText.name = "Sell Confirmation Text";
        sellConfirmationText.text = string.Empty;
        sellConfirmationText.fontSize = 26f;
        sellConfirmationText.enableAutoSizing = false;
        sellConfirmationText.alignment = TextAlignmentOptions.Center;
        sellConfirmationText.color = Color.white;
        sellConfirmationText.raycastTarget = false;
        RectTransform textRect = sellConfirmationText.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0.4f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(12f, 0f);
        textRect.offsetMax = new Vector2(-12f, -8f);

        confirmSellButton = CreateSellDialogButton("Confirm Sell", dialogRect,
            new Vector2(-85f, 25f), "确认出售", new Color(0.55f, 0.18f, 0.2f, 1f));
        cancelSellButton = CreateSellDialogButton("Cancel Sell", dialogRect,
            new Vector2(85f, 25f), "取消", new Color(0.25f, 0.32f, 0.4f, 1f));
        sellConfirmation.SetActive(false);
    }

    private Button CreateSellDialogButton(string name, RectTransform parent,
        Vector2 position, string labelText, Color backgroundColor)
    {
        Button button = Instantiate(sellButton, parent, false);
        button.name = name;
        button.onClick = new Button.ButtonClickedEvent();
        button.gameObject.SetActive(true);
        button.GetComponent<Image>().color = backgroundColor;
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(145f, 44f);
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        label.text = labelText;
        label.fontSize = 22f;
        return button;
    }

    private void PositionBuildChoices(BuildSlot slot, bool roadSite)
    {
        Camera uiCamera = hudCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : hudCanvas.worldCamera;
        Vector2 screenPosition = gameplayCamera.WorldToScreenPoint(slot.transform.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            hudRectTransform, screenPosition, uiCamera, out Vector2 localPosition);

        float buttonWidth = guardTowerButton.GetComponent<RectTransform>().rect.width;
        float buttonHeight = guardTowerButton.GetComponent<RectTransform>().rect.height;
        float groupWidth = roadSite ? buttonWidth : buttonWidth * 2f + 8f;
        Rect canvasRect = hudRectTransform.rect;
        float centerX = Mathf.Clamp(localPosition.x,
            canvasRect.xMin + groupWidth * 0.5f + 8f,
            canvasRect.xMax - groupWidth * 0.5f - 8f);
        float centerY = Mathf.Clamp(localPosition.y + slotActionButtonOffset,
            canvasRect.yMin + buttonHeight * 0.5f + 8f,
            canvasRect.yMax - buttonHeight * 0.5f - 8f);

        if (roadSite)
        {
            PositionBuildChoice(phagocyteButton, new Vector2(centerX, centerY));
            return;
        }

        float halfSpacing = (buttonWidth + 8f) * 0.5f;
        PositionBuildChoice(guardTowerButton, new Vector2(centerX - halfSpacing, centerY));
        PositionBuildChoice(interceptorTowerButton, new Vector2(centerX + halfSpacing, centerY));
    }

    private static void PositionBuildChoice(Button button, Vector2 position)
    {
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
    }

    private void RefreshBuildChoice(Button button, TowerDefinition definition)
    {
        bool affordable = economyService.CurrentATP >= definition.BuildCost;
        button.interactable = affordable;
        Image icon = button.transform.Find("Tower Icon").GetComponent<Image>();
        icon.color = affordable ? Color.white : new Color(1f, 1f, 1f, 0.45f);
    }

    public void ShowBuildPreview(TowerDefinition definition)
    {
        if (buildController == null || gameFlowController.CurrentState != GameState.Running)
        {
            return;
        }

        BuildSlot slot = buildController.SelectedSlot;
        if (slot == null || !slot.CanBuild() || definition == null ||
            slot.PlacementKind != definition.PlacementKind)
        {
            return;
        }

        hoveredBuildDefinition = definition;
        previewSlot = slot;
        RefreshVisibleBuildPreview();
    }

    public void HideBuildPreview(TowerDefinition definition)
    {
        if (hoveredBuildDefinition == definition)
        {
            ClearBuildPreview();
        }
    }

    private void RefreshVisibleBuildPreview()
    {
        if (hoveredBuildDefinition != null && previewSlot != null && towerBuildPreview != null)
        {
            towerBuildPreview.Show(previewSlot, hoveredBuildDefinition,
                economyService.CurrentATP >= hoveredBuildDefinition.BuildCost);
        }
    }

    private void ClearBuildPreview()
    {
        hoveredBuildDefinition = null;
        previewSlot = null;
        if (towerBuildPreview != null)
        {
            towerBuildPreview.Hide();
        }
    }

    private void CreateTowerSelectionButtons()
    {
        if (guardTowerButton != null || interceptorTowerButton != null || phagocyteButton != null)
        {
            return;
        }

        guardTowerButton = CreateTowerSelectionButton(guardTowerDefinition);
        interceptorTowerButton = CreateTowerSelectionButton(interceptorTowerDefinition);
        guardTowerButton.gameObject.SetActive(false);
        interceptorTowerButton.gameObject.SetActive(false);
        if (phagocyteDefinition != null)
        {
            phagocyteButton = CreateTowerSelectionButton(phagocyteDefinition);
            phagocyteButton.gameObject.SetActive(false);
        }
    }

    private Button CreateTowerSelectionButton(TowerDefinition definition)
    {
        Button button = Instantiate(pauseButton, pauseButton.transform.parent);
        button.name = $"{definition.DisplayName} Button";
        button.onClick = new Button.ButtonClickedEvent();

        RectTransform rectTransform = button.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(112f, 112f);

        Image background = button.GetComponent<Image>();
        background.color = new Color(0.15f, 0.19f, 0.28f, 0.92f);

        GameObject iconObject = new GameObject("Tower Icon", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        iconObject.layer = button.gameObject.layer;
        iconObject.transform.SetParent(button.transform, false);
        Image icon = iconObject.GetComponent<Image>();
        SpriteRenderer towerSprite = definition.Prefab.GetComponentInChildren<SpriteRenderer>();
        icon.sprite = definition.BuildIcon != null ? definition.BuildIcon : towerSprite.sprite;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0f, -5f);
        iconRect.sizeDelta = new Vector2(68f, 68f);

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        label.text = $"{definition.DisplayName}\n{definition.BuildCost} ATP";
        label.fontSize = 16f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 11f;
        label.fontSizeMax = 16f;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.anchoredPosition = new Vector2(0f, 3f);
        labelRect.sizeDelta = new Vector2(-4f, 34f);

        button.gameObject.AddComponent<TowerBuildChoiceHover>().Initialize(this, definition);

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
        sellConfirmationBackdrop.onClick.AddListener(CloseSellConfirmation);
        confirmSellButton.onClick.AddListener(HandleConfirmSellClicked);
        cancelSellButton.onClick.AddListener(CloseSellConfirmation);
        upgradeButton.onClick.AddListener(HandleUpgradeClicked);
        if (!useCustomResultPanels)
        {
            restartButton.onClick.AddListener(RestartLevel);
            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.AddListener(LoadNextLevel);
            }
            if (returnToMenuButton != null)
            {
                returnToMenuButton.onClick.AddListener(ReturnToMainMenu);
            }
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
        sellConfirmationBackdrop.onClick.RemoveListener(CloseSellConfirmation);
        confirmSellButton.onClick.RemoveListener(HandleConfirmSellClicked);
        cancelSellButton.onClick.RemoveListener(CloseSellConfirmation);
        upgradeButton.onClick.RemoveListener(HandleUpgradeClicked);
        if (!useCustomResultPanels)
        {
            restartButton.onClick.RemoveListener(RestartLevel);
            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.RemoveListener(LoadNextLevel);
            }
            if (returnToMenuButton != null)
            {
                returnToMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            }
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
