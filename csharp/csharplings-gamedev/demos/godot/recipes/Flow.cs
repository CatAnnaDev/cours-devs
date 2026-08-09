using Godot;

namespace Demos.Recipes;

public partial class PauseMenu : CanvasLayer
{
    private static readonly StringName TogglePause = "ui_cancel";

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Visible = false;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed(TogglePause))
            return;

        Toggle();
        GetViewport().SetInputAsHandled();
    }

    public void Toggle()
    {
        bool paused = !GetTree().Paused;

        GetTree().Paused = paused;
        Visible = paused;
    }

    public void Resume()
    {
        GetTree().Paused = false;
        Visible = false;
    }
}

public partial class SceneFader : CanvasLayer
{
    [Export] public float Duration { get; set; } = 0.35f;

    private ColorRect _veil;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Layer = 128;

        _veil = new ColorRect
        {
            Color = new Color(0f, 0f, 0f, 0f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

        _veil.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_veil);
    }

    public async void GoTo(string scenePath)
    {
        Tween fadeOut = CreateTween();

        fadeOut.TweenProperty(_veil, "color:a", 1f, Duration);

        await ToSignal(fadeOut, Tween.SignalName.Finished);

        Error error = GetTree().ChangeSceneToFile(scenePath);

        if (error != Error.Ok)
        {
            GD.PushError($"changement de scene impossible : {error}");

            return;
        }

        Tween fadeIn = CreateTween();

        fadeIn.TweenProperty(_veil, "color:a", 0f, Duration);
    }
}

public partial class LockedDoor : Area2D
{
    [Signal]
    public delegate void OpenedEventHandler();

    [Export] public string RequiredKey { get; set; } = "cle_rouge";
    [Export] private Node2D _visual;

    private bool _open;

    public override void _Ready() => BodyEntered += OnBodyEntered;

    public override void _ExitTree() => BodyEntered -= OnBodyEntered;

    private void OnBodyEntered(Node2D body)
    {
        if (_open)
            return;

        if (!body.HasMethod("HasKey"))
            return;

        if (!body.Call("HasKey", RequiredKey).AsBool())
        {
            GD.Print($"il manque {RequiredKey}");

            return;
        }

        _open = true;

        SetDeferred(PropertyName.Monitoring, false);

        if (_visual is not null)
            _visual.Visible = false;

        EmitSignal(SignalName.Opened);
    }
}

public partial class SaveSlot : Node
{
    private const string Path = "user://save_1.txt";

    public static bool Exists() => FileAccess.FileExists(Path);

    public static void Write(int level, float health, string checkpoint)
    {
        using FileAccess file = FileAccess.Open(Path, FileAccess.ModeFlags.Write);

        if (file is null)
        {
            GD.PushError($"sauvegarde impossible : {FileAccess.GetOpenError()}");

            return;
        }

        file.StoreLine("version=2");
        file.StoreLine($"level={level}");
        file.StoreLine($"health={health.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        file.StoreLine($"checkpoint={checkpoint}");
    }

    public static bool TryRead(out int level, out float health, out string checkpoint)
    {
        level = 1;
        health = 100f;
        checkpoint = "start";

        if (!Exists())
            return false;

        using FileAccess file = FileAccess.Open(Path, FileAccess.ModeFlags.Read);

        if (file is null)
            return false;

        while (!file.EofReached())
        {
            string[] parts = file.GetLine().Split('=', 2);

            if (parts.Length != 2)
                continue;

            switch (parts[0])
            {
                case "level":
                    int.TryParse(parts[1], out level);
                    break;

                case "health":
                    float.TryParse(parts[1], System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out health);
                    break;

                case "checkpoint":
                    checkpoint = parts[1];
                    break;
            }
        }

        return true;
    }
}

public partial class BulletPool : Node
{
    [Export] private PackedScene _bullet;
    [Export] private int _prewarm = 64;

    private readonly System.Collections.Generic.Stack<Node2D> _free = new();

    public int FreeCount => _free.Count;

    public override void _Ready()
    {
        if (_bullet is null)
        {
            GD.PushWarning("BulletPool sans PackedScene");

            return;
        }

        for (int i = 0; i < _prewarm; i++)
            _free.Push(Create());
    }

    public Node2D Take(Vector2 position, float rotation)
    {
        Node2D bullet = _free.Count > 0 ? _free.Pop() : Create();

        bullet.GlobalPosition = position;
        bullet.GlobalRotation = rotation;
        bullet.Visible = true;
        bullet.ProcessMode = ProcessModeEnum.Inherit;

        return bullet;
    }

    public void Give(Node2D bullet)
    {
        if (bullet is null || !IsInstanceValid(bullet))
            return;

        bullet.Visible = false;
        bullet.ProcessMode = ProcessModeEnum.Disabled;

        _free.Push(bullet);
    }

    private Node2D Create()
    {
        var bullet = _bullet.Instantiate<Node2D>();

        bullet.Visible = false;
        bullet.ProcessMode = ProcessModeEnum.Disabled;

        AddChild(bullet);

        return bullet;
    }
}
