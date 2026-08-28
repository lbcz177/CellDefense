using System;
using UnityEngine;

public class GameFlowController : MonoBehaviour
{
    public event Action<GameState> StateChanged;

    public GameState CurrentState { get; private set; }

    public void Initialize()
    {
        // TODO: Set the initial run state and required starting values.
    }

    public void StartRun()
    {
        // TODO: Start the playable run and publish the state change.
    }

    public void Pause()
    {
        // TODO: Pause gameplay through the single time-control entry point.
    }

    public void Resume()
    {
        // TODO: Resume gameplay through the single time-control entry point.
    }

    public void EvaluateResult()
    {
        // TODO: Decide whether the current run should continue, win, or lose.
    }
}
