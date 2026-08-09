namespace Csharplings;

public readonly struct Entity : IEquatable<Entity>
{
    public Entity(int index, int generation)
    {
        Index = index;
        Generation = generation;
    }

    public int Index { get; }
    public int Generation { get; }

    public static Entity None => new Entity(-1, 0);

    public bool Equals(Entity other) => Index == other.Index && Generation == other.Generation;

    public override bool Equals(object obj) => obj is Entity other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Index, Generation);

    public override string ToString() => $"#{Index}v{Generation}";
}

public sealed class EntityAllocator
{
    private readonly List<int> _generationBySlot = new();
    private readonly Stack<int> _freeSlots = new();

    public int AliveCount { get; private set; }

    public int SlotCount => _generationBySlot.Count;

    public Entity Create()
    {
        AliveCount++;

        if (_freeSlots.Count > 0)
        {
            int recycled = _freeSlots.Pop();

            return new Entity(recycled, _generationBySlot[recycled]);
        }

        _generationBySlot.Add(1);

        return new Entity(_generationBySlot.Count - 1, 1);
    }

    public bool IsAlive(Entity entity) =>
        entity.Index >= 0
        && entity.Index < _generationBySlot.Count
        && _generationBySlot[entity.Index] == entity.Generation;

    public bool Destroy(Entity entity)
    {
        if (!IsAlive(entity))
            return false;

        _generationBySlot[entity.Index]++;
        _freeSlots.Push(entity.Index);
        AliveCount--;

        return true;
    }
}

public static class Entities1
{
    public const bool NotDone = false;

    public static void Run()
    {
        var allocator = new EntityAllocator();

        Entity first = allocator.Create();
        Entity second = allocator.Create();

        Check.Equal(first.Index, 0, "la premiere entite prend le slot 0");
        Check.Equal(second.Index, 1, "la deuxieme prend le slot suivant");
        Check.Equal(allocator.AliveCount, 2, "deux entites en vie");
        Check.True(allocator.IsAlive(first), "la premiere repond present");

        Check.True(allocator.Destroy(first), "on la detruit");
        Check.Equal(allocator.AliveCount, 1, "il n'en reste qu'une");
        Check.False(allocator.IsAlive(first), "son identifiant ne resout plus");
        Check.False(allocator.Destroy(first), "et on ne peut pas la detruire deux fois");

        Entity recycled = allocator.Create();

        Check.Equal(recycled.Index, first.Index, "le slot libere est REUTILISE : le tableau reste sans trou");
        Check.Equal(allocator.SlotCount, 2, "on n'a donc pas agrandi le tableau pour rien");
        Check.True(recycled.Generation > first.Generation, "mais la generation du slot a change");
        Check.False(recycled.Equals(first), "donc l'ancien identifiant n'est PAS egal au nouveau");
        Check.True(allocator.IsAlive(recycled), "seul le nouveau resout");
        Check.False(allocator.IsAlive(first),
            "et l'ancien reste mort pour toujours, meme si son slot est reoccupe : c'est ca qui tue le pointeur fantome");

        Check.False(allocator.IsAlive(Entity.None), "l'entite vide ne resout jamais");
        Check.False(allocator.IsAlive(new Entity(999, 1)), "un index hors bornes non plus, et sans planter");
        Check.False(allocator.IsAlive(new Entity(0, 99)), "une generation inventee non plus");

        var byIdentifier = new Dictionary<Entity, string>
        {
            [first] = "le fantome",
            [second] = "la vivante",
            [recycled] = "la remplacante",
        };

        Check.Equal(byIdentifier.Count, 3, "les trois identifiants sont bien distincts comme cles de dictionnaire");
        Check.Equal(byIdentifier[recycled], "la remplacante", "et le nouveau ne pointe pas sur l'entree de l'ancien");
        Check.Equal(recycled.ToString(), "#0v2", "un identifiant se lit : slot 0, deuxieme occupant");

        var survivors = new List<Entity>();

        for (int i = 0; i < 8; i++)
            survivors.Add(allocator.Create());

        foreach (Entity entity in survivors)
        {
            if (entity.Index % 2 == 0)
                allocator.Destroy(entity);
        }

        int stillAlive = survivors.Count(allocator.IsAlive);

        Check.Equal(stillAlive, 4, "sur huit entites, les quatre aux slots impairs survivent");
        Check.Equal(allocator.AliveCount, 6, "plus les deux du debut restees en vie");
    }
}
