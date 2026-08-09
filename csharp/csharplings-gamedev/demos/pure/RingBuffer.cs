namespace Demos.Pure;

public sealed class RingBuffer<T>
{
    private readonly T[] _slots;
    private int _next;

    public RingBuffer(int capacity)
    {
        _slots = new T[Math.Max(capacity, 1)];
    }

    public int Capacity => _slots.Length;

    public int Count { get; private set; }

    public long TotalWritten { get; private set; }

    public T this[int fromNewest]
    {
        get
        {
            if (fromNewest < 0 || fromNewest >= Count)
                throw new ArgumentOutOfRangeException(nameof(fromNewest));

            int index = _next - 1 - fromNewest;

            if (index < 0)
                index += _slots.Length;

            return _slots[index];
        }
    }

    public T Newest => this[0];

    public T Oldest => this[Count - 1];

    public void Write(T value)
    {
        _slots[_next] = value;
        _next = _next + 1 == _slots.Length ? 0 : _next + 1;
        TotalWritten++;

        if (Count < _slots.Length)
            Count++;
    }

    public bool TryFind(Func<T, bool> predicate, out T found)
    {
        for (int i = 0; i < Count; i++)
        {
            if (!predicate(this[i]))
                continue;

            found = this[i];

            return true;
        }

        found = default;

        return false;
    }

    public void Clear()
    {
        Array.Clear(_slots, 0, _slots.Length);
        _next = 0;
        Count = 0;
    }
}

public readonly struct InputFrame
{
    public InputFrame(long tick, float horizontal, bool jump)
    {
        Tick = tick;
        Horizontal = horizontal;
        Jump = jump;
    }

    public long Tick { get; }

    public float Horizontal { get; }

    public bool Jump { get; }

    public override string ToString() => $"t{Tick}[{Horizontal:+0.0;-0.0;0.0}{(Jump ? " saut" : "")}]";
}

public static class RingBufferDemo
{
    public static void Demo()
    {
        Console.WriteLine("--- RingBuffer : historique a taille fixe, zero allocation apres construction ---");

        var history = new RingBuffer<InputFrame>(capacity: 8);

        for (long tick = 0; tick < 12; tick++)
            history.Write(new InputFrame(tick, tick % 3 - 1f, jump: tick % 5 == 0));

        Console.WriteLine($"  capacite {history.Capacity}, contient {history.Count}, ecrit en tout {history.TotalWritten}");
        Console.WriteLine($"  le plus recent : {history.Newest}");
        Console.WriteLine($"  le plus ancien : {history.Oldest}   (les quatre premiers ont ete ecrases, et c'est voulu)");

        Console.Write("  du plus recent au plus ancien :");

        for (int i = 0; i < history.Count; i++)
            Console.Write($" {history[i]}");

        Console.WriteLine();

        bool foundJump = history.TryFind(frame => frame.Jump, out InputFrame jump);

        Console.WriteLine($"  dernier saut retrouve dans la fenetre : {foundJump} {(foundJump ? jump.ToString() : "")}");
        Console.WriteLine("  c'est comme ca qu'on fait un buffer d'entree : on ne cherche que dans les N dernieres frames");

        var snapshots = new RingBuffer<int>(capacity: 4);

        for (int tick = 1; tick <= 6; tick++)
            snapshots.Write(tick * 10);

        Console.WriteLine($"  anneau de snapshots : {snapshots.Oldest} .. {snapshots.Newest}");
        Console.WriteLine("  en reseau on garde ainsi les N derniers etats pour pouvoir rejouer depuis le dernier confirme");
        Console.WriteLine("  et la memoire ne bouge JAMAIS : le tableau est alloue une fois, on ne fait que tourner dedans");
        Console.WriteLine();
    }
}
