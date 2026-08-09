using Godot;
using Godot.Collections;

namespace Demos.Recipes;

public partial class Snippets : Node2D
{
    private static readonly StringName Jump = "jump";
    private static readonly StringName Enemies = "enemies";
    private static readonly NodePath BarPath = "UI/Bar";

    [Export] private PackedScene _prefab;
    [Export] private Node2D _target;
    [Export] private Timer _timer;
    [Export] private AnimationPlayer _animator;
    [Export] private AudioStreamPlayer _sound;
    [Export] private Sprite2D _sprite;
    [Export] private Label _label;

    private readonly RandomNumberGenerator _rng = new();

    private void Nodes()
    {
        Node child = GetNodeOrNull<Label>(BarPath);
        Node unique = GetNodeOrNull<Label>("%HealthBar");
        Node parent = GetParentOrNull<Node2D>();
        Node player = GetTree().GetFirstNodeInGroup("player");
        Array<Node> all = GetTree().GetNodesInGroup(Enemies);

        AddToGroup(Enemies);
        RemoveFromGroup(Enemies);

        GD.Print(child, unique, parent, player, all.Count, IsInGroup(Enemies));
        GD.Print(GetTree().GetNodeCountInGroup(Enemies));
    }

    private void Spawning()
    {
        if (_prefab is null)
            return;

        var made = _prefab.Instantiate<Node2D>();

        AddChild(made);
        made.GlobalPosition = GlobalPosition;

        Node2D copy = (Node2D)made.Duplicate();

        GetTree().CurrentScene.AddChild(copy);

        made.Reparent(GetTree().CurrentScene);

        GD.Print(made.IsQueuedForDeletion(), made.GetIndex(), GetChildCount());
    }

    private void Deferred()
    {
        CallDeferred(MethodName.Nodes);
        SetDeferred(Node2D.PropertyName.Visible, false);
        Callable.From(Nodes).CallDeferred();
    }

    private void Toggling()
    {
        SetProcess(false);
        SetPhysicsProcess(false);
        SetProcessInput(false);
        ProcessMode = ProcessModeEnum.Disabled;
    }

    private void Inputs()
    {
        bool held = Input.IsActionPressed(Jump);
        bool tapped = Input.IsActionJustPressed(Jump);
        bool released = Input.IsActionJustReleased(Jump);
        float axis = Input.GetAxis("move_left", "move_right");
        Vector2 stick = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        float strength = Input.GetActionStrength(Jump);

        Input.MouseMode = Input.MouseModeEnum.Visible;

        Vector2 mouse = GetGlobalMousePosition();

        GD.Print(held, tapped, released, axis, stick, strength, mouse);
    }

    private void Screen()
    {
        Vector2 view = GetViewport().GetVisibleRect().Size;
        Vector2I window = DisplayServer.WindowGetSize();
        double fps = Engine.GetFramesPerSecond();
        double scale = Engine.TimeScale;
        bool debug = OS.IsDebugBuild();
        float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

        GD.Print(view, window, fps, scale, debug, gravity);
    }

    private async void Timing()
    {
        await ToSignal(GetTree().CreateTimer(1.0), SceneTreeTimer.SignalName.Timeout);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        if (_timer is not null)
        {
            _timer.WaitTime = 2.0;
            _timer.OneShot = true;
            _timer.Timeout += OnTimeout;
            _timer.Start();
            _timer.Stop();

            GD.Print(_timer.TimeLeft, _timer.IsStopped());
        }
    }

    private void OnTimeout() => GD.Print("timeout");

    private void Tweens()
    {
        Tween tween = CreateTween().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);

        tween.TweenProperty(this, "position", new Vector2(100f, 0f), 0.4);
        tween.Parallel().TweenProperty(this, "modulate:a", 0f, 0.4);
        tween.TweenInterval(0.2);
        tween.TweenCallback(Callable.From(QueueFree));

        tween.SetLoops(2);
        tween.Kill();
    }

    private void Animation()
    {
        if (_animator is null)
            return;

        _animator.Play("hit");
        _animator.AnimationFinished += OnAnimationFinished;

        GD.Print(_animator.IsPlaying(), _animator.CurrentAnimation);
    }

    private void OnAnimationFinished(StringName name) => GD.Print($"fini : {name}");

    private void Audio()
    {
        if (_sound is null)
            return;

        _sound.Play();
        _sound.Stop();
        _sound.VolumeDb = -6f;
        _sound.PitchScale = 1.2f;

        GD.Print(_sound.Playing);
    }

    private void Visuals()
    {
        if (_sprite is not null)
        {
            _sprite.FlipH = true;
            _sprite.Modulate = Colors.Red;
            _sprite.SelfModulate = Color.FromHsv(0.5f, 1f, 1f);
        }

        if (_label is not null)
            _label.Text = "PV 100";

        ZIndex = 10;
        Scale = Vector2.One * 1.5f;
    }

    private void Aiming()
    {
        if (_target is null)
            return;

        LookAt(_target.GlobalPosition);

        Rotation = (_target.GlobalPosition - GlobalPosition).Angle();

        float shortest = Mathf.AngleDifference(Rotation, (_target.GlobalPosition - GlobalPosition).Angle());

        Rotation = Mathf.LerpAngle(Rotation, Rotation + shortest, 0.1f);

        GD.Print(GlobalPosition.DistanceTo(_target.GlobalPosition));
        GD.Print(GlobalPosition.DirectionTo(_target.GlobalPosition));
    }

    private void MathHelpers()
    {
        GD.Print(Mathf.Wrap(370f, 0f, 360f));
        GD.Print(Mathf.Snapped(37f, 16f));
        GD.Print(Mathf.PingPong(5f, 3f));
        GD.Print(Mathf.MoveToward(0f, 10f, 3f));
        GD.Print(Mathf.Lerp(0f, 10f, 0.5f));
        GD.Print(Mathf.InverseLerp(0f, 10f, 5f));
        GD.Print(Mathf.Clamp(15, 0, 10));
        GD.Print(Mathf.PosMod(-1, 4));

        Vector2 slid = new Vector2(1f, 1f).Slide(Vector2.Up);
        Vector2 bounced = new Vector2(1f, 1f).Bounce(Vector2.Up);
        Vector2 reflected = new Vector2(1f, 1f).Reflect(Vector2.Right);
        Vector2I cell = new Vector2I(3, 4);

        GD.Print(slid, bounced, reflected, cell);
    }

    private void Randomness()
    {
        _rng.Seed = 1234;
        _rng.Randomize();

        GD.Print(_rng.RandiRange(1, 6), _rng.RandfRange(0f, 1f), _rng.Randf());
        GD.Print(GD.RandRange(1, 6), GD.Randf(), GD.Randi() % 6);
    }

    private void SceneFlow()
    {
        GetTree().ReloadCurrentScene();
        GetTree().ChangeSceneToFile("res://scenes/menu.tscn");
        GetTree().Paused = true;
        GetTree().Quit();
    }

    private void Resources()
    {
        var texture = ResourceLoader.Load<Texture2D>("res://art/hero.png");
        var scene = GD.Load<PackedScene>("res://scenes/enemy.tscn");

        GD.Print(texture, scene, ResourceLoader.Exists("res://art/hero.png"));
    }

    private void Diagnostics()
    {
        GD.Print("valeur");
        GD.PrintErr("erreur");
        GD.PushWarning("avertissement");
        GD.PushError("erreur bloquante");
        GD.PrintRich("[b]gras[/b]");

        GetTree().DebugCollisionsHint = true;

        GD.Print(Performance.GetMonitor(Performance.Monitor.ObjectNodeCount));
        GD.Print(Performance.GetMonitor(Performance.Monitor.MemoryStatic));
    }
}

public partial class ExportForms : Node2D
{
    [ExportCategory("Combat")]
    [Export] public float Damage { get; set; } = 10f;

    [ExportGroup("Mouvement")]
    [Export(PropertyHint.Range, "0,600,10")] public int Speed { get; set; } = 200;
    [Export(PropertyHint.Range, "0,1,0.05")] public float Friction { get; set; } = 0.2f;

    [ExportGroup("References")]
    [Export] public PackedScene Bullet { get; set; }
    [Export] public NodePath TargetPath { get; set; }
    [Export] public Node2D Target { get; set; }
    [Export] public Texture2D Icon { get; set; }

    [ExportGroup("Texte et fichiers")]
    [Export(PropertyHint.MultilineText)] public string Dialogue { get; set; } = string.Empty;
    [Export(PropertyHint.File, "*.tscn")] public string ScenePath { get; set; } = string.Empty;
    [Export(PropertyHint.Dir)] public string Folder { get; set; } = string.Empty;

    [ExportGroup("Divers")]
    [Export] public Godot.Collections.Array<string> Tags { get; set; } = new();
    [Export] public Godot.Collections.Dictionary Extra { get; set; } = new();
    [Export(PropertyHint.Layers2DPhysics)] public uint Mask { get; set; } = 1;
    [Export(PropertyHint.ColorNoAlpha)] public Color Tint { get; set; } = Colors.White;
    [Export] public Tween.TransitionType Curve { get; set; } = Tween.TransitionType.Cubic;
    [Export] public Vector2I Cell { get; set; } = Vector2I.Zero;
}
