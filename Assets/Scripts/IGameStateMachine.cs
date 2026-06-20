using System;

public enum GameState
{
    Playing,
    Paused,
    GameOver
}

public interface IGameStateMachine
{
    event Action<GameState> StateChanged;
    GameState CurrentState { get; }
    bool IsPlaying { get; }
    void SetPlaying();
    void SetPaused();
    void SetGameOver();
}
