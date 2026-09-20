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

    private EconomyService economyService;
    private LifeService lifeService;
    private GameFlowController gameFlowController;
    private bool isSubscribed;

    public void Initialize(
        EconomyService economy,
        LifeService life,
        GameFlowController gameFlow)
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

        economyService = economy;
        lifeService = life;
        gameFlowController = gameFlow;

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
        stateText.text = state switch
        {
            GameState.Boot => "状态：启动中",
            GameState.Ready => "状态：准备中",
            GameState.Running => "状态：进行中",
            GameState.Paused => "状态：已暂停",
            GameState.Victory => "状态：胜利",
            GameState.Defeat => "状态：失败",
            _ => $"状态：{state}"
        };

        pauseButton.interactable =
            state == GameState.Running || state == GameState.Paused;

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

    private void Subscribe()
    {
        if (isSubscribed || economyService == null || lifeService == null ||
            gameFlowController == null || pauseButton == null)
        {
            return;
        }

        economyService.ATPChanged += RefreshATP;
        lifeService.LifeChanged += RefreshLife;
        gameFlowController.StateChanged += RefreshState;
        pauseButton.onClick.AddListener(HandlePauseClicked);
        restartButton.onClick.AddListener(HandleRestartClicked);
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
        pauseButton.onClick.RemoveListener(HandlePauseClicked);
        restartButton.onClick.RemoveListener(HandleRestartClicked);
        isSubscribed = false;
    }
}
