using System;

public sealed class GameStateMachine : IGameStateMachine
{
    public event Action<GameState> StateChanged;

    public GameState CurrentState { get; private set; } = GameState.Playing;
    public bool IsPlaying => CurrentState == GameState.Playing;

    public void SetPlaying()
    {
        SetState(GameState.Playing);
    }

    public void SetPaused()
    {
        SetState(GameState.Paused);
    }

    public void SetGameOver()
    {
        SetState(GameState.GameOver);
    }

    private void SetState(GameState state)
    {
        if (CurrentState == state)
            return;

        CurrentState = state;
        StateChanged?.Invoke(CurrentState);
    }
}
