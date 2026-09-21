using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

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
    private GameObject resultPanel;
    [SerializeField]
    private TextMeshProUGUI resultText;
    [SerializeField]
    private Button restartButton;
    private Button guardTowerButton;
    private Button interceptorTowerButton;
    [SerializeField]
    private TowerDefinition guardTowerDefinition;
    [SerializeField]
    private TowerDefinition interceptorTowerDefinition;

    private EconomyService economyService;
    private LifeService lifeService;
    private GameFlowController gameFlowController;
    private WaveController waveController;
    private BuildController buildController;
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

        economyService = economy;
        lifeService = life;
        gameFlowController = gameFlow;
        waveController = waves;
        buildController = build;
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

    private void RefreshATP(int currentATP)
    {
        if (atpText == null)
        {
            throw new InvalidOperationException("ATP Text reference is not set in the inspector.");
        }
        atpText.text = $"ATP: {currentATP}";
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
        string selectionLabel = buildController != null && buildController.SelectedDefinition != null
            ? $"  当前塔：{buildController.SelectedDefinition.DisplayName}"
            : "  当前塔：未选择";
        stateText.text = stateLabel + waveLabel + selectionLabel;

        pauseButton.interactable =
            state == GameState.Running || state == GameState.Paused;

        bool canSelectTower = state == GameState.Ready ||
                              state == GameState.Running ||
                              state == GameState.Paused;
        guardTowerButton.interactable = canSelectTower;
        interceptorTowerButton.interactable = canSelectTower;

        bool hasResult = state == GameState.Victory || state == GameState.Defeat;
        resultPanel.SetActive(hasResult);
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

    private void HandleWaveChanged(int currentWave, int totalWaves)
    {
        RefreshState(gameFlowController.CurrentState);
    }

    private void HandleGuardTowerClicked()
    {
        buildController.TrySelectDefinition(guardTowerDefinition);
    }

    private void HandleInterceptorTowerClicked()
    {
        buildController.TrySelectDefinition(interceptorTowerDefinition);
    }

    private void HandleSelectedDefinitionChanged(TowerDefinition definition)
    {
        RefreshState(gameFlowController.CurrentState);
    }

    private void CreateTowerSelectionButtons()
    {
        if (guardTowerButton != null || interceptorTowerButton != null)
        {
            return;
        }

        guardTowerButton = CreateTowerSelectionButton(guardTowerDefinition, new Vector2(20f, 20f));
        interceptorTowerButton = CreateTowerSelectionButton(interceptorTowerDefinition, new Vector2(200f, 20f));
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
            pauseButton == null || guardTowerButton == null || interceptorTowerButton == null)
        {
            return;
        }

        economyService.ATPChanged += RefreshATP;
        lifeService.LifeChanged += RefreshLife;
        gameFlowController.StateChanged += RefreshState;
        waveController.WaveChanged += HandleWaveChanged;
        buildController.SelectedDefinitionChanged += HandleSelectedDefinitionChanged;
        pauseButton.onClick.AddListener(HandlePauseClicked);
        restartButton.onClick.AddListener(HandleRestartClicked);
        guardTowerButton.onClick.AddListener(HandleGuardTowerClicked);
        interceptorTowerButton.onClick.AddListener(HandleInterceptorTowerClicked);
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
        buildController.SelectedDefinitionChanged -= HandleSelectedDefinitionChanged;
        pauseButton.onClick.RemoveListener(HandlePauseClicked);
        restartButton.onClick.RemoveListener(HandleRestartClicked);
        guardTowerButton.onClick.RemoveListener(HandleGuardTowerClicked);
        interceptorTowerButton.onClick.RemoveListener(HandleInterceptorTowerClicked);
        isSubscribed = false;
    }
}
