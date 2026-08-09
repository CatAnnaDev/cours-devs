namespace Csharplings;

public sealed class Bullet
{
    public float TravelledDistance { get; set; }
    public bool InFlight { get; set; }

    public void Reset()
    {
        TravelledDistance = 0f;
        InFlight = false;
    }
}

public sealed class BulletPool
{
    private readonly Stack<Bullet> _available = new();

    public int TotalCreated { get; private set; }
    public int AvailableCount => _available.Count;

    public Bullet Take()
    {
        Bullet bullet;

        if (_available.Count > 0)
        {
            bullet = _available.Pop();
        }
        else
        {
            bullet = new Bullet();
            TotalCreated++;
        }

        bullet.InFlight = true;
        return bullet;
    }

    public void Give(Bullet bullet)
    {
        bullet.Reset();
        _available.Push(bullet);
    }
}

public static class Pool1
{
    public const bool NotDone = false;

    public static void Run()
    {
        var pool = new BulletPool();

        Bullet a = pool.Take();
        Bullet b = pool.Take();
        Bullet c = pool.Take();

        Check.Equal(pool.TotalCreated, 3, "la premiere salve fabrique 3 balles");
        Check.True(a.InFlight, "une balle sortie du pool est en vol");

        a.TravelledDistance = 500f;
        pool.Give(a);
        pool.Give(b);
        pool.Give(c);

        Check.Equal(pool.AvailableCount, 3, "les 3 balles sont revenues au stock");

        Bullet recycled = pool.Take();

        Check.Equal(pool.TotalCreated, 3, "la salve suivante ne fabrique RIEN, elle recycle");
        Check.Near(recycled.TravelledDistance, 0.0, "une balle recyclee repart a zero");
        Check.True(recycled.InFlight, "et elle est bien en vol");
        Check.Equal(pool.AvailableCount, 2, "il reste 2 balles en stock");

        pool.Take();
        pool.Take();
        pool.Take();

        Check.Equal(pool.TotalCreated, 4, "quand le stock est vide, on en fabrique une de plus");
    }
}
