using Godot;

namespace Demos.Singletons;

public partial class SingletonUsage : Node
{
    private GameState _gameState;

    public override void _Ready()
    {
        _gameState = GetNode<GameState>("/root/GameState");
        _gameState.ScoreChanged += OnScoreChanged;

        GameState.Instance.Score += 10;

        SaveService.Instance.Set("last_level", SceneRoutes.Arena);
        SaveService.Instance.Save();
    }

    public override void _ExitTree()
    {
        if (_gameState != null && GodotObject.IsInstanceValid(_gameState))
            _gameState.ScoreChanged -= OnScoreChanged;
    }

    private void OnScoreChanged(int newScore)
    {
        GD.Print($"score is now {newScore}");
    }
}
