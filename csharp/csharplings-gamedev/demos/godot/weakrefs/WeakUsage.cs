using System;
using Godot;

namespace Demos.WeakRefs;

public readonly struct ScoreChanged
{
    public ScoreChanged(int total)
    {
        Total = total;
    }

    public int Total { get; }
}

public partial class CorrectSubscriber : Node
{
    private readonly WeakEventBus<ScoreChanged> _bus;

    public CorrectSubscriber(WeakEventBus<ScoreChanged> bus)
    {
        _bus = bus;
    }

    public int Seen { get; private set; }

    public override void _Ready() =>
        _bus.Subscribe(this, static (self, message) => self.OnScore(message));

    private void OnScore(ScoreChanged message)
    {
        Seen++;

        GD.Print($"score recu : {message.Total}");
    }
}

public partial class LeakingSubscriber : Node
{
    private readonly WeakEventBus<ScoreChanged> _bus;

    public LeakingSubscriber(WeakEventBus<ScoreChanged> bus)
    {
        _bus = bus;
    }

    public int Seen { get; private set; }

    public override void _Ready() =>
        _bus.Subscribe(this, (_, message) => Count(message));

    private void Count(ScoreChanged message) => Seen++;
}

public partial class DeterministicSubscriber : Node
{
    private readonly WeakEventBus<ScoreChanged> _bus;

    public DeterministicSubscriber(WeakEventBus<ScoreChanged> bus)
    {
        _bus = bus;
    }

    public override void _Ready() =>
        _bus.Subscribe(this, static (self, message) => self.OnScore(message));

    public override void _ExitTree() => _bus.Unsubscribe(this);

    private void OnScore(ScoreChanged message) => GD.Print($"score : {message.Total}");
}

public partial class NodeReferenceWays : Node
{
    [Export] private NodePath _targetPath;

    private ulong _targetId;
    private Node _cached;

    public override void _Ready()
    {
        _cached = GetNodeOrNull<Node>(_targetPath);

        if (_cached is not null)
            _targetId = _cached.GetInstanceId();
    }

    public Node ByPath() => GetNodeOrNull<Node>(_targetPath);

    public Node ByCachedReference() => GodotObject.IsInstanceValid(_cached) ? _cached : null;

    public Node ByInstanceId()
    {
        if (!GodotObject.IsInstanceIdValid(_targetId))
        {
            _targetId = 0UL;

            return null;
        }

        return GodotObject.InstanceFromId(_targetId) as Node;
    }

    public Node ByGodotWeakRef(WeakRef weak)
    {
        Variant target = weak.GetRef();

        return target.VariantType == Variant.Type.Nil ? null : target.As<Node>();
    }

    public Node ByManagedWeakReference(WeakReference<Node> weak) =>
        weak.TryGetTarget(out Node node) && GodotObject.IsInstanceValid(node) ? node : null;
}
