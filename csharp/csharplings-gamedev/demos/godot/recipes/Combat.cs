using Godot;
using Godot.Collections;

namespace Demos.Recipes;

public partial class Bullet : Area2D
{
    [Export] public float Speed { get; set; } = 1200f;
    [Export] public float Damage { get; set; } = 10f;
    [Export] public float Lifetime { get; set; } = 3f;

    private Array<Rid> _exclude;
    private double _age;

    public override void _Ready() => _exclude = new Array<Rid> { GetRid() };

    public override void _PhysicsProcess(double delta)
    {
        _age += delta;

        if (_age > Lifetime)
        {
            QueueFree();

            return;
        }

        Vector2 from = GlobalPosition;
        Vector2 to = from + Vector2.Right.Rotated(GlobalRotation) * Speed * (float)delta;

        PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(from, to, CollisionMask, _exclude);
        Dictionary hit = GetWorld2D().DirectSpaceState.IntersectRay(query);

        if (hit.Count == 0)
        {
            GlobalPosition = to;

            return;
        }

        GlobalPosition = hit["position"].AsVector2();

        if (hit["collider"].As<Node>() is Node target)
            GD.Print($"touche {target.Name} pour {Damage}");

        QueueFree();
    }
}

public partial class AreaDamage : Node2D
{
    [Export] public float Radius { get; set; } = 96f;
    [Export] public float Damage { get; set; } = 30f;
    [Export] public uint TargetLayers { get; set; } = 1;

    private readonly CircleShape2D _shape = new();
    private readonly PhysicsShapeQueryParameters2D _query = new();

    public override void _Ready()
    {
        _shape.Radius = Radius;
        _query.Shape = _shape;
        _query.CollisionMask = TargetLayers;
        _query.CollideWithAreas = true;
        _query.CollideWithBodies = true;
    }

    public int Explode()
    {
        _query.Transform = new Transform2D(0f, GlobalPosition);

        Array<Dictionary> hits = GetWorld2D().DirectSpaceState.IntersectShape(_query, maxResults: 32);
        float squared = Radius * Radius;
        int touched = 0;

        foreach (Dictionary hit in hits)
        {
            if (hit["collider"].As<Node2D>() is not Node2D body)
                continue;

            if (body.GlobalPosition.DistanceSquaredTo(GlobalPosition) > squared)
                continue;

            GD.Print($"{body.Name} prend {Damage}");
            touched++;
        }

        return touched;
    }
}

public partial class Ability : Node
{
    [Signal]
    public delegate void UsedEventHandler();

    [Signal]
    public delegate void ReadyAgainEventHandler();

    [Export] public float Cooldown { get; set; } = 1.5f;

    private float _remaining;

    public bool IsReady => _remaining <= 0f;

    public float Progress => Cooldown <= 0f ? 1f : 1f - _remaining / Cooldown;

    public override void _Process(double delta)
    {
        if (_remaining <= 0f)
            return;

        _remaining = Mathf.Max(_remaining - (float)delta, 0f);

        if (_remaining <= 0f)
            EmitSignal(SignalName.ReadyAgain);
    }

    public bool TryUse()
    {
        if (!IsReady)
            return false;

        _remaining = Cooldown;

        EmitSignal(SignalName.Used);

        return true;
    }
}

public partial class QuietHud : Node
{
    [Export] private Label _label;
    [Export] private ProgressBar _bar;

    private int _shownHealth = -1;
    private string _cached = string.Empty;

    public void SetHealth(int current, int max)
    {
        if (_bar is not null)
        {
            _bar.MaxValue = max;
            _bar.Value = current;
        }

        if (_label is null || current == _shownHealth)
            return;

        _shownHealth = current;
        _cached = $"PV {current} / {max}";
        _label.Text = _cached;
    }
}
