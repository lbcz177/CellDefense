using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowController : MonoBehaviour
{
    public event Action<GameState> StateChanged;

    public GameState CurrentState { get; private set; }
    private LifeService lifeService;
    private WaveController waveController;
    private bool waveCompleted;

    public void Initialize(LifeService lifeService, WaveController waveController)
    {
        if (lifeService == null)
        {
            throw new ArgumentNullException(nameof(lifeService));
        }
        if (waveController == null)
        {
            throw new ArgumentNullException(nameof(waveController));
        }

        this.lifeService = lifeService;
        this.waveController = waveController;
        waveCompleted = false;
        lifeService.LifeChanged += HandleLifeChanged;
        waveController.WaveCompleted += HandleWaveCompleted;
        SetState(GameState.Ready);
    }

    public void StartRun()
    {
        if (CurrentState != GameState.Ready)
        {
            return;
        }

        Time.timeScale = 1f;
        SetState(GameState.Running);
        waveController.StartWave();
    }

    public void Pause()
    {
        if (CurrentState != GameState.Running)
        {
            return;
        }

        Time.timeScale = 0f;
        SetState(GameState.Paused);
    }

    public void Resume()
    {
        if (CurrentState != GameState.Paused)
        {
            return;
        }

        Time.timeScale = 1f;
        SetState(GameState.Running);
    }

    public void RestartRun()
    {
        if (CurrentState != GameState.Victory && CurrentState != GameState.Defeat)
        {
            return;
        }
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void EvaluateResult()
    {
        if (CurrentState != GameState.Running)
        {
            return;
        }
        if (lifeService.CurrentLife <= 0)
        {
            Time.timeScale = 0f;
            SetState(GameState.Defeat);
        }
        else if (waveCompleted)
        {
            Time.timeScale = 0f;
            SetState(GameState.Victory);
        }
    }

    private void HandleLifeChanged(int currentLife)
    {
        EvaluateResult();
    }

    private void HandleWaveCompleted()
    {
        waveCompleted = true;
        EvaluateResult();
    }

    private void SetState(GameState state)
    {
        if (CurrentState == state)
        {
            return;
        }

        CurrentState = state;
        Debug.Log($"Game state changed to {CurrentState}.");
        StateChanged?.Invoke(CurrentState);
    }

    private void OnDestroy()
    {
        if (lifeService != null)
        {
            lifeService.LifeChanged -= HandleLifeChanged;
        }
        if (waveController != null)
        {
            waveController.WaveCompleted -= HandleWaveCompleted;
        }
    }
}
