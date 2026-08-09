namespace Csharplings;

public readonly struct Contact : IEquatable<Contact>
{
    public Contact(int id, float distance)
    {
        Id = id;
        Distance = distance;
    }

    public int Id { get; }

    public float Distance { get; }

    public bool Equals(Contact other) => Id == other.Id && Distance == other.Distance;

    public override bool Equals(object obj) => obj is Contact other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Id, Distance);

    public override string ToString() => $"{Id}@{Distance:0.##}";
}

public sealed class OverlapWorld
{
    private readonly Vector2[] _centers;
    private readonly float[] _radii;

    public OverlapWorld(Vector2[] centers, float[] radii)
    {
        _centers = centers;
        _radii = radii;
    }

    public int Count => _centers.Length;

    public int QueryCircle(Vector2 center, float radius, Span<Contact> results)
    {
        int found = 0;

        for (int id = 0; id < _centers.Length; id++)
        {
            if (found == results.Length)
                return found;

            float reach = radius + _radii[id];
            float distanceSquared = _centers[id].DistanceSquaredTo(center);

            if (distanceSquared > reach * reach)
                continue;

            results[found] = new Contact(id, Mathf.Sqrt(distanceSquared));
            found++;
        }

        return found;
    }

    public List<Contact> QueryCircleAllocating(Vector2 center, float radius)
    {
        var results = new List<Contact>();

        for (int id = 0; id < _centers.Length; id++)
        {
            float reach = radius + _radii[id];
            float distanceSquared = _centers[id].DistanceSquaredTo(center);

            if (distanceSquared <= reach * reach)
                results.Add(new Contact(id, Mathf.Sqrt(distanceSquared)));
        }

        return results.OrderBy(contact => contact.Distance).ToList();
    }

    public bool CouldTouch(Vector2 center, float radius, int id)
    {
        float reach = radius + _radii[id];
        var box = new Rect2(_centers[id] - Vector2.One * reach, Vector2.One * reach * 2f);

        return box.HasPoint(center);
    }

    public static void SortByDistance(Span<Contact> contacts)
    {
        for (int i = 1; i < contacts.Length; i++)
        {
            Contact moving = contacts[i];
            int slot = i - 1;

            while (slot >= 0 && contacts[slot].Distance > moving.Distance)
            {
                contacts[slot + 1] = contacts[slot];
                slot--;
            }

            contacts[slot + 1] = moving;
        }
    }
}

public static class Probe1
{
    public const bool NotDone = false;

    private static readonly Contact[] Buffer = new Contact[8];

    private static readonly OverlapWorld World = new(
        new[]
        {
            new Vector2(30f, 0f),
            new Vector2(10f, 0f),
            new Vector2(0f, 20f),
            new Vector2(200f, 200f),
            new Vector2(0f, -5f),
        },
        new[] { 1f, 1f, 1f, 1f, 1f });

    private static int _lastCount;
    private static object _keepsItAlive;

    public static void Run()
    {
        int count = Probe(Vector2.Zero, 21f);

        Check.Equal(count, 3, "trois cercles a portee sur les cinq du monde");
        Check.Sequence(Slice(count), new[] { new Contact(4, 5f), new Contact(1, 10f), new Contact(2, 20f) },
            "et le tampon est trie du plus proche au plus loin, en place, sans rien allouer");

        Check.Equal(Probe(Vector2.Zero, 5f), 1, "un rayon plus petit ne trouve que le voisin immediat");
        Check.Equal(Probe(new Vector2(1000f, 1000f), 5f), 0, "et loin de tout, aucun contact");

        Span<Contact> tiny = Buffer.AsSpan(0, 2);

        Check.Equal(World.QueryCircle(Vector2.Zero, 21f, tiny), 2,
            "ATTENTION : un tampon trop petit rend ce qu'il peut et abandonne le reste EN SILENCE. C'est le piege des API sans allocation des deux moteurs");
        Check.True(World.QueryCircle(Vector2.Zero, 21f, tiny) < 3,
            "il faut donc soit un tampon dimensionne pour le pire cas, soit relancer avec plus grand");

        for (int id = 0; id < World.Count; id++)
        {
            bool precise = Precise(Vector2.Zero, 21f, id);

            Check.True(!precise || World.CouldTouch(Vector2.Zero, 21f, id),
                "regle absolue du filtrage grossier : il peut proposer trop, il ne doit JAMAIS oublier un vrai contact");
        }

        Check.True(World.CouldTouch(Vector2.Zero, 21f, 2), "la boite englobante du cercle 2 attrape bien l'origine");
        Check.False(World.CouldTouch(Vector2.Zero, 21f, 3), "celle du cercle lointain, non : c'est tout le gain");

        Check.Equal(Report("une requete plus un tri, tampon fourni", Allocations(() => _lastCount = Probe(Vector2.Zero, 21f))), 0L,
            "un tampon fourni par l'appelant plus un tri en place : ZERO octet par requete, meme a soixante requetes par frame");

        long allocating = Report("la meme requete en List plus OrderBy",
            Allocations(() => _keepsItAlive = World.QueryCircleAllocating(Vector2.Zero, 21f)));

        Check.True(allocating > 0L,
            "la version confortable alloue sa liste, son tri et son enumerateur : c'est exactement ce que fait un IntersectRay qui rend un dictionnaire");
    }

    private static int Probe(Vector2 center, float radius)
    {
        int found = World.QueryCircle(center, radius, Buffer);

        OverlapWorld.SortByDistance(Buffer.AsSpan(0, found));

        return found;
    }

    private static bool Precise(Vector2 center, float radius, int id)
    {
        Span<Contact> one = Buffer.AsSpan(0, Buffer.Length);
        int found = World.QueryCircle(center, radius, one);

        for (int i = 0; i < found; i++)
        {
            if (one[i].Id == id)
                return true;
        }

        return false;
    }

    private static Contact[] Slice(int count) => Buffer.AsSpan(0, count).ToArray();

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
