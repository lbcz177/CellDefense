using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowController : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";

    public event Action<GameState> StateChanged;

    [SerializeField]
    private string nextLevelSceneName;
    public GameState CurrentState { get; private set; }
    public bool HasNextLevel
    {
        get
        {
            return !string.IsNullOrWhiteSpace(nextLevelSceneName) &&
                   !string.Equals(SceneManager.GetActiveScene().name, nextLevelSceneName, StringComparison.OrdinalIgnoreCase) &&
                   Application.CanStreamedLevelBeLoaded(nextLevelSceneName);
        }
    }
    private LifeService lifeService;
    private WaveController waveController;
    private bool allWavesCompleted;

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
        allWavesCompleted = false;
        lifeService.LifeChanged += HandleLifeChanged;
        waveController.AllWavesCompleted += HandleAllWavesCompleted;
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
        waveController.StartSequence();
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

    public bool TryLoadNextLevel()
    {
        if (CurrentState != GameState.Victory || !HasNextLevel)
        {
            return false;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(nextLevelSceneName);
        return true;
    }

    public bool TryReturnToMainMenu()
    {
        if (CurrentState != GameState.Victory && CurrentState != GameState.Defeat)
        {
            return false;
        }
        if (!Application.CanStreamedLevelBeLoaded(MainMenuSceneName))
        {
            throw new InvalidOperationException("MainMenu scene is not included in Scenes In Build.");
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(MainMenuSceneName);
        return true;
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
        else if (allWavesCompleted)
        {
            Time.timeScale = 0f;
            SetState(GameState.Victory);
        }
    }

    private void HandleLifeChanged(int currentLife)
    {
        EvaluateResult();
    }

    private void HandleAllWavesCompleted()
    {
        allWavesCompleted = true;
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
            waveController.AllWavesCompleted -= HandleAllWavesCompleted;
        }
    }
}
