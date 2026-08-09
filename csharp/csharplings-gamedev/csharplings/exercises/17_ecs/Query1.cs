namespace Csharplings;

public enum Trait
{
    Position,
    Velocity,
    Health,
    Sprite,
}

public readonly struct TraitMask
{
    private TraitMask(ulong bits)
    {
        Bits = bits;
    }

    public ulong Bits { get; }

    public static TraitMask Of(params Trait[] traits)
    {
        ulong bits = 0UL;

        foreach (Trait trait in traits)
            bits |= 1UL << (int)trait;

        return new TraitMask(bits);
    }

    public TraitMask With(Trait trait) => new TraitMask(Bits | 1UL << (int)trait);

    public bool HasAll(TraitMask required) => (Bits & required.Bits) == required.Bits;
}

public sealed class TraitWorld
{
    private TraitMask[] _masks = new TraitMask[4];

    public int Count { get; private set; }

    public int Spawn(params Trait[] traits)
    {
        if (Count == _masks.Length)
            Array.Resize(ref _masks, _masks.Length * 2);

        _masks[Count] = TraitMask.Of(traits);

        return Count++;
    }

    public void Add(int entity, Trait trait) => _masks[entity] = _masks[entity].With(trait);

    public TraitMask MaskOf(int entity) => _masks[entity];

    public Query With(TraitMask required) => new Query(_masks, Count, required);

    public IEnumerable<int> WithYield(TraitMask required)
    {
        for (int entity = 0; entity < Count; entity++)
        {
            if (_masks[entity].HasAll(required))
                yield return entity;
        }
    }
}

public readonly struct Query
{
    private readonly TraitMask[] _masks;
    private readonly int _count;
    private readonly TraitMask _required;

    public Query(TraitMask[] masks, int count, TraitMask required)
    {
        _masks = masks;
        _count = count;
        _required = required;
    }

    public Enumerator GetEnumerator() => new Enumerator(_masks, _count, _required);

    public sealed class Enumerator
    {
        private readonly TraitMask[] _masks;
        private readonly int _count;
        private readonly TraitMask _required;
        private int _index;

        public Enumerator(TraitMask[] masks, int count, TraitMask required)
        {
            _masks = masks;
            _count = count;
            _required = required;
            _index = -1;
        }

        public int Current => _index;

        public bool MoveNext() => ++_index < _count;
    }
}

public static class Query1
{
    public const bool NotDone = true;

    public static void Run()
    {
        var world = new TraitWorld();

        int walker = world.Spawn(Trait.Position, Trait.Velocity, Trait.Sprite);
        int rock = world.Spawn(Trait.Position, Trait.Sprite);
        int fighter = world.Spawn(Trait.Position, Trait.Velocity, Trait.Health);
        int ghost = world.Spawn();
        int wind = world.Spawn(Trait.Velocity);

        Check.Equal(world.Count, 5, "cinq entites dans le monde");
        Check.Equal(walker, 0, "la premiere porte l'index 0");
        Check.Equal(wind, 4, "la derniere l'index 4");

        TraitMask movers = TraitMask.Of(Trait.Position, Trait.Velocity);

        Check.Sequence(Collect(world.With(movers)), new[] { walker, fighter },
            "la requete Position+Velocity ne rend que les deux entites qui portent LES DEUX");
        Check.Sequence(Collect(world.With(TraitMask.Of(Trait.Position))), new[] { walker, rock, fighter },
            "une requete a un seul composant en rend trois");
        Check.Sequence(Collect(world.With(TraitMask.Of())), new[] { walker, rock, fighter, ghost, wind },
            "une requete vide rend tout le monde, y compris l'entite sans aucun composant");
        Check.Sequence(Collect(world.With(TraitMask.Of(Trait.Health, Trait.Sprite))), Array.Empty<int>(),
            "et une requete que personne ne satisfait rend une suite vide, pas un null");

        world.Add(rock, Trait.Velocity);

        Check.Sequence(Collect(world.With(movers)), new[] { walker, rock, fighter },
            "ajouter un composant fait entrer l'entite dans la requete, sans rien reindexer");

        Check.Sequence(Collect(world.With(movers)), world.WithYield(movers).ToList(),
            "les deux versions rendent exactement la meme chose");

        Check.Equal(Report("un foreach sur la requete", Allocations(() => Walk(world, movers))), 0L,
            "un foreach sur la requete alloue ZERO octet : l'enumerateur est une structure, il vit sur la pile");
        Check.True(Report("la meme boucle en yield return", Allocations(() => WalkWithYield(world, movers))) > 0L,
            "la meme boucle en 'yield return' alloue son enumerateur sur le tas, a chaque appel");

        Check.Equal(Walk(world, movers), 3, "les deux comptent pareil");
        Check.Equal(WalkWithYield(world, movers), 3, "seule la facture memoire change");
    }

    private static int Walk(TraitWorld world, TraitMask required)
    {
        int seen = 0;

        foreach (int entity in world.With(required))
            seen++;

        return seen;
    }

    private static int WalkWithYield(TraitWorld world, TraitMask required)
    {
        int seen = 0;

        foreach (int entity in world.WithYield(required))
            seen++;

        return seen;
    }

    private static List<int> Collect(Query query)
    {
        var found = new List<int>();

        foreach (int entity in query)
            found.Add(entity);

        return found;
    }

    private static long Allocations(Action action)
    {
        action();
        action();
        action();

        long before = GC.GetAllocatedBytesForCurrentThread();
        action();

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static long Report(string what, long bytes)
    {
        Console.WriteLine($"      mesure  {what} : {bytes} octets");

        return bytes;
    }
}
