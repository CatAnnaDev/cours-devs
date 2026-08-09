using Godot;
using Godot.Collections;

namespace Demos.Data;

[GlobalClass]
public partial class EnemyDefinition : Resource
{
    [Export] public string DisplayName { get; set; } = "Slime";
    [Export] public int MaxHealth { get; set; } = 30;
    [Export] public float Speed { get; set; } = 60f;
    [Export(PropertyHint.Range, "0,10,0.5")] public float AttackCooldown { get; set; } = 1.5f;
    [Export] public PackedScene Visual { get; set; }
    [Export] public Array<string> Tags { get; set; } = new();
}

public sealed class EnemyInstance
{
    public EnemyInstance(EnemyDefinition definition)
    {
        Definition = definition;
        Health = definition.MaxHealth;
    }

    public EnemyDefinition Definition { get; }

    public int Health { get; private set; }

    public Vector2 Position { get; set; }

    public bool IsAlive => Health > 0;

    public void TakeDamage(int amount) => Health = Mathf.Max(Health - amount, 0);
}

public partial class Spawner : Node2D
{
    [Export] public EnemyDefinition Definition { get; set; }
    [Export] public int Count { get; set; } = 500;

    private readonly System.Collections.Generic.List<EnemyInstance> _alive = new();

    public override void _Ready()
    {
        if (Definition is null)
        {
            GD.PushWarning("Spawner sans definition assignee");

            return;
        }

        for (int i = 0; i < Count; i++)
            _alive.Add(new EnemyInstance(Definition) { Position = new Vector2(i * 8f, 0f) });

        GD.Print($"{_alive.Count} ennemis instancies, et UNE seule fiche de stats en memoire");
        GD.Print($"tous partagent la meme reference : {ReferenceEquals(_alive[0].Definition, _alive[^1].Definition)}");
    }

    public override void _PhysicsProcess(double delta)
    {
        float speed = Definition?.Speed ?? 0f;

        for (int i = 0; i < _alive.Count; i++)
        {
            EnemyInstance enemy = _alive[i];

            if (!enemy.IsAlive)
                continue;

            enemy.Position += Vector2.Right * speed * (float)delta;
        }
    }
}

public partial class ThreadedLoad : Node
{
    [Export] public string ScenePath { get; set; } = "res://scenes/arena.tscn";

    [Signal]
    public delegate void ProgressedEventHandler(float ratio);

    [Signal]
    public delegate void FinishedEventHandler();

    private readonly Array _progress = new();
    private bool _requested;

    public override void _Ready()
    {
        Error error = ResourceLoader.LoadThreadedRequest(ScenePath);

        if (error != Error.Ok)
        {
            GD.PushError($"chargement impossible : {error}");

            return;
        }

        _requested = true;
    }

    public override void _Process(double delta)
    {
        if (!_requested)
            return;

        ResourceLoader.ThreadLoadStatus status = ResourceLoader.LoadThreadedGetStatus(ScenePath, _progress);

        switch (status)
        {
            case ResourceLoader.ThreadLoadStatus.InProgress:
                EmitSignal(SignalName.Progressed, _progress.Count > 0 ? _progress[0].AsSingle() : 0f);
                break;

            case ResourceLoader.ThreadLoadStatus.Loaded:
                _requested = false;
                OnLoaded((PackedScene)ResourceLoader.LoadThreadedGet(ScenePath));
                break;

            default:
                _requested = false;
                GD.PushError($"chargement echoue : {status}");
                break;
        }
    }

    private void OnLoaded(PackedScene scene)
    {
        Node instance = scene.Instantiate();

        GetTree().Root.AddChild(instance);
        EmitSignal(SignalName.Finished);

        GD.Print("la scene est arrivee sans avoir gele une seule frame");
    }
}
