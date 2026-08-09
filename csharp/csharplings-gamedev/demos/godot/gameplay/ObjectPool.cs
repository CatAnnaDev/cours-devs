using System.Collections.Generic;
using Godot;

namespace Demos.Gameplay;

public interface IPoolable
{
    void OnTakenFromPool();
    void OnReturnedToPool();
}

public partial class ObjectPool : Node
{
    [Export] private PackedScene _prefab;
    [Export] private int _prewarmCount = 32;

    private readonly Stack<Node> _available = new();
    private readonly HashSet<Node> _inUse = new();

    public int AvailableCount => _available.Count;
    public int InUseCount => _inUse.Count;

    public override void _Ready()
    {
        if (_prefab == null)
        {
            GD.PushWarning("ObjectPool sans prefab assigne");
            return;
        }

        for (int i = 0; i < _prewarmCount; i++)
            _available.Push(CreateInstance());

        GD.Print($"pool prechauffee : {AvailableCount} objets prets, zero allocation pendant le jeu");
    }

    public Node Take()
    {
        Node node = _available.Count > 0 ? _available.Pop() : CreateInstance();

        _inUse.Add(node);
        node.ProcessMode = ProcessModeEnum.Inherit;

        if (node is Node2D node2D)
            node2D.Visible = true;

        if (node is IPoolable poolable)
            poolable.OnTakenFromPool();

        return node;
    }

    public void Give(Node node)
    {
        if (node == null || !IsInstanceValid(node) || !_inUse.Remove(node))
            return;

        if (node is IPoolable poolable)
            poolable.OnReturnedToPool();

        node.ProcessMode = ProcessModeEnum.Disabled;

        if (node is Node2D node2D)
            node2D.Visible = false;

        _available.Push(node);
    }

    private Node CreateInstance()
    {
        Node node = _prefab.Instantiate();
        node.ProcessMode = ProcessModeEnum.Disabled;
        AddChild(node);

        if (node is Node2D node2D)
            node2D.Visible = false;

        return node;
    }
}

public partial class PooledBullet : Node2D, IPoolable
{
    [Export] public float Speed { get; set; } = 600f;

    private double _lifetime;

    public void OnTakenFromPool()
    {
        _lifetime = 0.0;
    }

    public void OnReturnedToPool()
    {
        Position = Vector2.Zero;
    }

    public override void _PhysicsProcess(double delta)
    {
        _lifetime += delta;
        Position += Vector2.Right.Rotated(Rotation) * Speed * (float)delta;

        if (_lifetime > 3.0)
            GetParent<ObjectPool>().Give(this);
    }
}
