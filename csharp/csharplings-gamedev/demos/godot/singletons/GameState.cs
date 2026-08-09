using Godot;

namespace Demos.Singletons;

public partial class GameState : Node
{
    public static GameState Instance { get; private set; }

    [Signal]
    public delegate void ScoreChangedEventHandler(int newScore);

    private int _score;

    public int Score
    {
        get => _score;
        set
        {
            if (_score == value)
                return;

            _score = value;
            EmitSignal(SignalName.ScoreChanged, _score);
        }
    }

    public override void _EnterTree()
    {
        if (Instance != null && Instance != this)
        {
            QueueFree();
            return;
        }

        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ResetRun()
    {
        Score = 0;
    }
}
