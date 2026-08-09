namespace Csharplings;

public sealed class Recorder : Node
{
    private bool _processLogged;

    public List<string> Log { get; } = new();

    public Recorder()
    {
        Log.Add("constructeur");
    }

    public override void _EnterTree()
    {
        Log.Add("_EnterTree");
    }

    public override void _Ready()
    {
        Log.Add("_Ready");
    }

    public override void _Process(double delta)
    {
        if (_processLogged)
            return;

        _processLogged = true;
        Log.Add("_Process");
    }

    public override void _ExitTree()
    {
        Log.Add("_ExitTree");
    }
}

public static class Godot1
{
    public const bool NotDone = false;

    public static void Run()
    {
        var tree = new SceneTree();
        var recorder = new Recorder { Name = "Recorder" };

        tree.Root.AddChild(recorder);
        tree.Start();
        tree.Tick();

        recorder.QueueFree();
        tree.Tick();

        Check.Sequence(
            recorder.Log,
            new[] { "constructeur", "_EnterTree", "_Ready", "_Process", "_ExitTree" },
            "l'ordre reel du cycle de vie d'un Node");
    }
}
