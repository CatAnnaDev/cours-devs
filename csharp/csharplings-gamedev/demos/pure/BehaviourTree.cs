namespace Demos.Pure;

public enum BtStatus
{
    Success,
    Failure,
    Running,
}

public sealed class Blackboard
{
    private readonly Dictionary<string, object> _values = new();

    public T Get<T>(string key, T fallback = default) =>
        _values.TryGetValue(key, out object stored) && stored is T typed ? typed : fallback;

    public void Set<T>(string key, T value) => _values[key] = value;

    public bool Has(string key) => _values.ContainsKey(key);
}

public abstract class BtNode
{
    public abstract BtStatus Tick(Blackboard board);

    public virtual void Reset() { }
}

public sealed class Condition : BtNode
{
    private readonly string _label;
    private readonly Func<Blackboard, bool> _test;

    public Condition(string label, Func<Blackboard, bool> test)
    {
        _label = label;
        _test = test;
    }

    public override BtStatus Tick(Blackboard board) => _test(board) ? BtStatus.Success : BtStatus.Failure;

    public override string ToString() => _label;
}

public sealed class Do : BtNode
{
    private readonly string _label;
    private readonly Func<Blackboard, BtStatus> _body;

    public Do(string label, Func<Blackboard, BtStatus> body)
    {
        _label = label;
        _body = body;
    }

    public override BtStatus Tick(Blackboard board)
    {
        BtStatus status = _body(board);

        board.Set("trace", board.Get("trace", string.Empty) + _label + " ");

        return status;
    }

    public override string ToString() => _label;
}

public sealed class Sequence : BtNode
{
    private readonly BtNode[] _children;
    private int _current;

    public Sequence(params BtNode[] children)
    {
        _children = children;
    }

    public override BtStatus Tick(Blackboard board)
    {
        while (_current < _children.Length)
        {
            BtStatus status = _children[_current].Tick(board);

            if (status == BtStatus.Running)
                return BtStatus.Running;

            if (status == BtStatus.Failure)
            {
                Reset();

                return BtStatus.Failure;
            }

            _current++;
        }

        Reset();

        return BtStatus.Success;
    }

    public override void Reset()
    {
        _current = 0;

        foreach (BtNode child in _children)
            child.Reset();
    }
}

public sealed class Selector : BtNode
{
    private readonly BtNode[] _children;
    private int _current;

    public Selector(params BtNode[] children)
    {
        _children = children;
    }

    public override BtStatus Tick(Blackboard board)
    {
        while (_current < _children.Length)
        {
            BtStatus status = _children[_current].Tick(board);

            if (status == BtStatus.Running)
                return BtStatus.Running;

            if (status == BtStatus.Success)
            {
                Reset();

                return BtStatus.Success;
            }

            _current++;
        }

        Reset();

        return BtStatus.Failure;
    }

    public override void Reset()
    {
        _current = 0;

        foreach (BtNode child in _children)
            child.Reset();
    }
}

public sealed class Inverter : BtNode
{
    private readonly BtNode _child;

    public Inverter(BtNode child)
    {
        _child = child;
    }

    public override BtStatus Tick(Blackboard board) =>
        _child.Tick(board) switch
        {
            BtStatus.Success => BtStatus.Failure,
            BtStatus.Failure => BtStatus.Success,
            _ => BtStatus.Running,
        };

    public override void Reset() => _child.Reset();
}

public sealed class Cooldown : BtNode
{
    private readonly BtNode _child;
    private readonly float _duration;
    private float _remaining;

    public Cooldown(float duration, BtNode child)
    {
        _duration = duration;
        _child = child;
    }

    public override BtStatus Tick(Blackboard board)
    {
        float delta = board.Get("delta", 0f);

        _remaining = MathF.Max(_remaining - delta, 0f);

        if (_remaining > 0f)
            return BtStatus.Failure;

        BtStatus status = _child.Tick(board);

        if (status == BtStatus.Success)
            _remaining = _duration;

        return status;
    }

    public override void Reset() => _child.Reset();
}

public static class BehaviourTreeDemo
{
    public static void Demo()
    {
        Console.WriteLine("--- BehaviourTree : le garde qui patrouille, poursuit et tire ---");

        BtNode brain = new Selector(
            new Sequence(
                new Condition("voit le joueur", board => board.Get("distance", 999f) < 200f),
                new Selector(
                    new Sequence(
                        new Condition("a portee", board => board.Get("distance", 999f) < 60f),
                        new Cooldown(1f, new Do("TIRE", board => board.Get("ammo", 0) > 0 ? BtStatus.Success : BtStatus.Failure))),
                    new Do("poursuit", _ => BtStatus.Success))),
            new Do("patrouille", _ => BtStatus.Success));

        var board = new Blackboard();

        board.Set("ammo", 3);
        board.Set("delta", 0.5f);

        float[] distances = { 500f, 500f, 150f, 150f, 40f, 40f, 40f, 40f, 300f };

        for (int tick = 0; tick < distances.Length; tick++)
        {
            board.Set("distance", distances[tick]);
            board.Set("trace", string.Empty);

            BtStatus status = brain.Tick(board);

            Console.WriteLine($"  distance {distances[tick],5:0} -> {board.Get("trace", "(rien)"),-14} [{status}]");
        }

        Console.WriteLine("  a 40 pixels il tire, puis le cooldown le fait retomber sur 'poursuit' une frame sur deux");
        Console.WriteLine("  l'interet contre une machine a etats : on ajoute une branche sans toucher aux transitions");
        Console.WriteLine();
    }
}
