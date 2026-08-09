using System;
using System.Runtime.CompilerServices;
using Godot;

namespace Demos.WeakRefs;

public sealed class TargetMetadata
{
    public int TimesTargeted { get; set; }
    public double LastSeenAt { get; set; }
}

public partial class GodotObjectLifetime : Node
{
    private static readonly ConditionalWeakTable<Node, TargetMetadata> Metadata = new();

    private WeakReference<Node> _weakTarget;
    private Node _strongTarget;

    public override void _Ready()
    {
        _strongTarget = new Node { Name = "Enemy" };
        AddChild(_strongTarget);

        _weakTarget = new WeakReference<Node>(_strongTarget);
        Metadata.GetOrCreateValue(_strongTarget).TimesTargeted = 3;

        DescribeTarget("avant free");

        _strongTarget.QueueFree();
        _strongTarget = null;

        CallDeferred(MethodName.DescribeTarget, "apres queue_free");
    }

    private void DescribeTarget(string step)
    {
        if (!_weakTarget.TryGetTarget(out Node node))
        {
            GD.Print($"[{step}] wrapper C# collecte");
            return;
        }

        if (!GodotObject.IsInstanceValid(node))
        {
            GD.Print($"[{step}] wrapper C# vivant mais objet natif libere");
            return;
        }

        int targeted = Metadata.TryGetValue(node, out TargetMetadata meta) ? meta.TimesTargeted : 0;
        GD.Print($"[{step}] {node.Name} valide, cible {targeted} fois");
    }

    public static Node ResolveOrForget(ref WeakReference<Node> slot)
    {
        if (slot == null)
            return null;

        if (!slot.TryGetTarget(out Node node) || !GodotObject.IsInstanceValid(node))
        {
            slot = null;
            return null;
        }

        return node;
    }
}
