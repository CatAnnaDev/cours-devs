namespace Csharplings;

public static class GD
{
    public static void Print(object what) => Console.WriteLine($"      [jeu] {what}");
}

public static class Mathf
{
    public const float Pi = 3.14159265f;
    public const float Tau = 6.2831853f;

    public static float Max(float a, float b) => a > b ? a : b;
    public static float Min(float a, float b) => a < b ? a : b;
    public static int Max(int a, int b) => a > b ? a : b;
    public static int Min(int a, int b) => a < b ? a : b;
    public static float Abs(float a) => a < 0f ? -a : a;
    public static int Abs(int a) => a < 0 ? -a : a;
    public static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;
    public static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;
    public static float Lerp(float from, float to, float weight) => from + (to - from) * weight;
    public static bool IsEqualApprox(float a, float b) => Abs(a - b) < 0.00001f;
    public static bool IsZeroApprox(float a) => Abs(a) < 0.00001f;
    public static float Sqrt(float a) => (float)Math.Sqrt(a);
    public static float Exp(float a) => (float)Math.Exp(a);
    public static float Pow(float a, float b) => (float)Math.Pow(a, b);
    public static float Sin(float a) => (float)Math.Sin(a);
    public static float Cos(float a) => (float)Math.Cos(a);
    public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
    public static int Sign(float a) => Math.Sign(a);
    public static float Floor(float a) => (float)Math.Floor(a);
    public static float Ceil(float a) => (float)Math.Ceiling(a);
    public static float Round(float a) => (float)Math.Round(a, MidpointRounding.AwayFromZero);
    public static int FloorToInt(float a) => (int)Math.Floor(a);
    public static int CeilToInt(float a) => (int)Math.Ceiling(a);
    public static int RoundToInt(float a) => (int)Math.Round(a, MidpointRounding.AwayFromZero);
    public static float DegToRad(float degrees) => degrees * Pi / 180f;
    public static float RadToDeg(float radians) => radians * 180f / Pi;
    public static float Snapped(float value, float step) => IsZeroApprox(step) ? value : Round(value / step) * step;

    public static int PosMod(int value, int modulus)
    {
        int rest = value % modulus;
        return rest < 0 ? rest + Abs(modulus) : rest;
    }

    public static float PosMod(float value, float modulus)
    {
        float rest = value % modulus;
        return rest < 0f ? rest + Abs(modulus) : rest;
    }

    public static float Wrap(float value, float min, float max)
    {
        float range = max - min;
        return IsZeroApprox(range) ? min : min + PosMod(value - min, range);
    }

    public static float AngleDifference(float from, float to) => Wrap(to - from, -Pi, Pi);

    public static float LerpAngle(float from, float to, float weight) => from + AngleDifference(from, to) * weight;

    public static float MoveToward(float from, float to, float delta)
    {
        if (Abs(to - from) <= delta)
            return to;

        return from + Sign(to - from) * delta;
    }
}

public readonly struct Vector2 : IEquatable<Vector2>
{
    public float X { get; }
    public float Y { get; }

    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public static Vector2 Zero => new(0f, 0f);
    public static Vector2 One => new(1f, 1f);
    public static Vector2 Right => new(1f, 0f);
    public static Vector2 Left => new(-1f, 0f);
    public static Vector2 Up => new(0f, -1f);
    public static Vector2 Down => new(0f, 1f);

    public static Vector2 FromAngle(float radians) => new(Mathf.Cos(radians), Mathf.Sin(radians));

    public float Length() => Mathf.Sqrt(X * X + Y * Y);

    public float LengthSquared() => X * X + Y * Y;

    public Vector2 Normalized()
    {
        float length = Length();
        return Mathf.IsZeroApprox(length) ? Zero : new Vector2(X / length, Y / length);
    }

    public float DistanceTo(Vector2 other) => (other - this).Length();

    public float DistanceSquaredTo(Vector2 other) => (other - this).LengthSquared();

    public float Dot(Vector2 other) => X * other.X + Y * other.Y;

    public float Cross(Vector2 other) => X * other.Y - Y * other.X;

    public float Angle() => Mathf.Atan2(Y, X);

    public float AngleTo(Vector2 other) => Mathf.AngleDifference(Angle(), other.Angle());

    public Vector2 Rotated(float radians)
    {
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(X * cos - Y * sin, X * sin + Y * cos);
    }

    public Vector2 Lerp(Vector2 to, float weight) => new(Mathf.Lerp(X, to.X, weight), Mathf.Lerp(Y, to.Y, weight));

    public Vector2 MoveToward(Vector2 to, float delta)
    {
        Vector2 offset = to - this;
        return offset.Length() <= delta ? to : this + offset.Normalized() * delta;
    }

    public Vector2 Abs() => new(Mathf.Abs(X), Mathf.Abs(Y));

    public bool IsEqualApprox(Vector2 other) => Mathf.IsEqualApprox(X, other.X) && Mathf.IsEqualApprox(Y, other.Y);

    public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2 operator -(Vector2 a) => new(-a.X, -a.Y);
    public static Vector2 operator *(Vector2 a, float s) => new(a.X * s, a.Y * s);
    public static Vector2 operator *(float s, Vector2 a) => new(a.X * s, a.Y * s);
    public static Vector2 operator /(Vector2 a, float s) => new(a.X / s, a.Y / s);
    public static bool operator ==(Vector2 a, Vector2 b) => a.Equals(b);
    public static bool operator !=(Vector2 a, Vector2 b) => !a.Equals(b);

    public bool Equals(Vector2 other) => X == other.X && Y == other.Y;

    public override bool Equals(object obj) => obj is Vector2 other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Y);

    public override string ToString() => $"({X:0.##}, {Y:0.##})";
}

public readonly struct Rect2 : IEquatable<Rect2>
{
    public Vector2 Position { get; }
    public Vector2 Size { get; }

    public Rect2(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
    }

    public Rect2(float x, float y, float width, float height)
        : this(new Vector2(x, y), new Vector2(width, height)) { }

    public Vector2 End => Position + Size;
    public Vector2 Center => Position + Size / 2f;

    public bool HasPoint(Vector2 point) =>
        point.X >= Position.X && point.X < End.X && point.Y >= Position.Y && point.Y < End.Y;

    public bool Intersects(Rect2 other) =>
        Position.X < other.End.X && End.X > other.Position.X &&
        Position.Y < other.End.Y && End.Y > other.Position.Y;

    public bool Equals(Rect2 other) => Position.Equals(other.Position) && Size.Equals(other.Size);

    public override bool Equals(object obj) => obj is Rect2 other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Position, Size);

    public override string ToString() => $"[{Position} {Size}]";
}

public sealed class StringName
{
    public StringName(string value)
    {
        Value = value;
        Created++;
    }

    public static int Created { get; private set; }

    public string Value { get; }

    public static void ResetCounter() => Created = 0;

    public static implicit operator StringName(string value) => new StringName(value);

    public override string ToString() => Value;
}

public readonly struct Variant
{
    private readonly double _number;
    private readonly object _reference;

    private Variant(double number, object reference)
    {
        _number = number;
        _reference = reference;
    }

    public static Variant From(int value) => new Variant(value, null);

    public static Variant From(float value) => new Variant(value, null);

    public static Variant From(string value) => new Variant(0.0, value);

    public static implicit operator Variant(int value) => From(value);

    public static implicit operator Variant(float value) => From(value);

    public static implicit operator Variant(string value) => From(value);

    public int AsInt() => (int)_number;

    public float AsFloat() => (float)_number;

    public string AsString() => _reference as string ?? string.Empty;

    public override string ToString() => _reference as string ?? _number.ToString();
}

public sealed class GodotArray
{
    private readonly List<Variant> _items;

    public GodotArray()
    {
        _items = new List<Variant>();
    }

    private GodotArray(List<Variant> items)
    {
        _items = items;
    }

    public static int Conversions { get; private set; }

    public int Count => _items.Count;

    public Variant this[int index] => _items[index];

    public static void ResetCounter() => Conversions = 0;

    public void Add(Variant value) => _items.Add(value);

    public static GodotArray FromIntList(List<int> values)
    {
        Conversions++;

        var copied = new List<Variant>(values.Count);

        for (int i = 0; i < values.Count; i++)
            copied.Add(values[i]);

        return new GodotArray(copied);
    }

    public List<int> ToIntList()
    {
        Conversions++;

        var copied = new List<int>(_items.Count);

        for (int i = 0; i < _items.Count; i++)
            copied.Add(_items[i].AsInt());

        return copied;
    }
}

public sealed class SignalBus
{
    private readonly Dictionary<string, List<Action<Variant[]>>> _handlers = new();

    public static int EmitCalls { get; private set; }

    public static void ResetCounter() => EmitCalls = 0;

    public void Connect(StringName signal, Action<Variant[]> handler)
    {
        if (!_handlers.TryGetValue(signal.Value, out List<Action<Variant[]>> list))
        {
            list = new List<Action<Variant[]>>();
            _handlers[signal.Value] = list;
        }

        list.Add(handler);
    }

    public void Emit(StringName signal, params Variant[] arguments)
    {
        EmitCalls++;

        if (!_handlers.TryGetValue(signal.Value, out List<Action<Variant[]>> list))
            return;

        for (int i = 0; i < list.Count; i++)
            list[i](arguments);
    }
}

public class Node
{
    private static readonly List<Action> DeferredCalls = new();

    private readonly List<Node> _children = new();

    private Vector2 _position;

    public string Name { get; set; } = "Node";
    public Node Parent { get; private set; }
    public bool IsFreed { get; private set; }
    public bool IsInsideTree { get; private set; }

    public static int PropertyCrossings { get; set; }

    public static int DeferredPending => DeferredCalls.Count;

    public Vector2 Position
    {
        get
        {
            PropertyCrossings++;

            return _position;
        }
        set
        {
            PropertyCrossings++;
            _position = value;
        }
    }

    public void CallDeferred(Action call) => DeferredCalls.Add(call);

    internal static void FlushDeferred()
    {
        while (DeferredCalls.Count > 0)
        {
            Action call = DeferredCalls[0];

            DeferredCalls.RemoveAt(0);
            call();
        }
    }

    internal bool QueuedForDeletion { get; private set; }

    public IReadOnlyList<Node> Children => _children;

    public virtual void _EnterTree() { }
    public virtual void _Ready() { }
    public virtual void _Process(double delta) { }
    public virtual void _PhysicsProcess(double delta) { }
    public virtual void _ExitTree() { }

    public int ProcessPriority { get; set; }

    public void AddChild(Node child)
    {
        child.Parent = this;
        _children.Add(child);

        if (IsInsideTree)
            child.EnterTreeRecursive();
    }

    public static int LookupCount { get; set; }

    public T GetNodeOrNull<T>(string path)
        where T : Node
    {
        LookupCount++;

        Node current = this;

        foreach (string part in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            current = current._children.FirstOrDefault(c => c.Name == part);
            if (current is null)
                return null;
        }

        return current as T;
    }

    public T GetNode<T>(string path)
        where T : Node
    {
        T found = GetNodeOrNull<T>(path);
        if (found is null)
            throw new InvalidOperationException($"noeud introuvable : {path}");

        return found;
    }

    public void QueueFree() => QueuedForDeletion = true;

    public static bool IsInstanceValid(Node node) => node is not null && !node.IsFreed;

    internal void EnterTreeRecursive()
    {
        IsInsideTree = true;
        _EnterTree();

        foreach (Node child in _children.ToList())
            child.EnterTreeRecursive();

        _Ready();
    }

    internal void CollectRecursive(List<Node> into)
    {
        if (IsFreed)
            return;

        into.Add(this);

        foreach (Node child in _children.ToList())
            child.CollectRecursive(into);
    }

    internal void FlushDeletionsRecursive()
    {
        foreach (Node child in _children.ToList())
        {
            child.FlushDeletionsRecursive();

            if (!child.QueuedForDeletion)
                continue;

            child._ExitTree();
            child.IsFreed = true;
            child.IsInsideTree = false;
            _children.Remove(child);
        }
    }
}

public sealed class SceneTree
{
    private double _accumulator;

    public Node Root { get; } = new() { Name = "root" };

    public double PhysicsStep { get; set; } = 1.0 / 60.0;

    public void Start()
    {
        Root.EnterTreeRecursive();
    }

    public void Tick(double delta = 1.0 / 60.0)
    {
        _accumulator += delta;

        while (_accumulator >= PhysicsStep)
        {
            _accumulator -= PhysicsStep;

            foreach (Node node in Collect())
                node._PhysicsProcess(PhysicsStep);
        }

        foreach (Node node in Collect().OrderBy(node => node.ProcessPriority))
            node._Process(delta);

        Node.FlushDeferred();
        Root.FlushDeletionsRecursive();
    }

    private List<Node> Collect()
    {
        var nodes = new List<Node>();
        Root.CollectRecursive(nodes);

        return nodes;
    }

    public void Run(int frames, double delta = 1.0 / 60.0)
    {
        for (int i = 0; i < frames; i++)
            Tick(delta);
    }
}
