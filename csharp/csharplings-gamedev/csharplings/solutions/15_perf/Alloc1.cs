namespace Csharplings;

public sealed record Enemy(string Name, Vector2 Position, int Health);

public static class Alloc1
{
    public const bool NotDone = false;

    public static long Measure(Action action)
    {
        action();
        action();
        action();

        long before = GC.GetAllocatedBytesForCurrentThread();
        action();

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    public static int TotalHealth(List<Enemy> enemies)
    {
        int total = 0;

        for (int i = 0; i < enemies.Count; i++)
            total += enemies[i].Health;

        return total;
    }

    public static int CountAlive(List<Enemy> enemies)
    {
        int alive = 0;

        foreach (Enemy enemy in enemies)
        {
            if (enemy.Health > 0)
                alive++;
        }

        return alive;
    }

    public static Enemy Nearest(List<Enemy> enemies, Vector2 from)
    {
        Enemy best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < enemies.Count; i++)
        {
            float distance = enemies[i].Position.DistanceSquaredTo(from);

            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            best = enemies[i];
        }

        return best;
    }

    public static void Run()
    {
        var enemies = new List<Enemy>();

        for (int i = 0; i < 200; i++)
            enemies.Add(new Enemy($"gobelin{i}", new Vector2(i * 10f, 0f), i % 3 == 0 ? 0 : 10));

        Check.Equal(TotalHealth(enemies), 1330, "le total des points de vie");
        Check.Equal(CountAlive(enemies), 133, "le nombre d'ennemis encore debout");
        Check.Equal(Nearest(enemies, new Vector2(45f, 0f)).Name, "gobelin4", "le plus proche d'un point");

        Check.Equal(Measure(() => TotalHealth(enemies)), 0L,
            "une boucle for sur une List n'alloue RIEN");

        Check.Equal(Measure(() => CountAlive(enemies)), 0L,
            "et foreach sur une List non plus : son enumerateur est un struct");

        Check.Equal(Measure(() => Nearest(enemies, Vector2.Zero)), 0L,
            "chercher le plus proche a la main : zero octet, 60 fois par seconde");

        Check.True(Measure(() => enemies.Where(enemy => enemy.Health > 0).Count()) > 0L,
            "alors que le meme calcul en LINQ fabrique des objets a chaque appel");

        Check.True(Measure(() => enemies.OrderBy(enemy => enemy.Position.X).First()) > 0L,
            "et un OrderBy trie TOUTE la liste pour n'en garder qu'un");

        long linq = Measure(() => enemies.Where(enemy => enemy.Health > 0).Count());
        long loop = Measure(() => CountAlive(enemies));

        Check.True(linq > loop, "sur un chemin chaud, la boucle gagne toujours");
    }
}
