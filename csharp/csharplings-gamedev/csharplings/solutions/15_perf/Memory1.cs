namespace Csharplings;

public sealed class ParticleObject
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Life;
}

public struct ParticleValue
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Life;
}

public static class Memory1
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

    public static void UpdateAll(ParticleValue[] particles, float delta)
    {
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].Position += particles[i].Velocity * delta;
            particles[i].Life -= delta;
        }
    }

    public static void Respawn(List<ParticleObject> particles, int count)
    {
        particles.Clear();

        for (int i = 0; i < count; i++)
            particles.Add(new ParticleObject { Velocity = new Vector2(1f, 0f), Life = 1f });
    }

    public static void Run()
    {
        var list = new List<ParticleValue> { new() { Life = 5f } };
        ParticleValue copy = list[0];
        copy.Life = 99f;

        Check.Near(list[0].Life, 5.0,
            "list[0] rend une COPIE de la structure : la modifier ne touche pas la liste");

        var array = new ParticleValue[1];
        array[0].Life = 5f;
        array[0].Life = 99f;

        Check.Near(array[0].Life, 99.0,
            "alors qu'un element de tableau est une vraie variable : on l'ecrit en place, sans copie");

        var pool = new ParticleValue[1000];

        for (int i = 0; i < pool.Length; i++)
        {
            pool[i].Velocity = new Vector2(60f, 0f);
            pool[i].Life = 2f;
        }

        UpdateAll(pool, 0.5f);

        Check.Near(pool[0].Position, new Vector2(30f, 0f), "les particules ont bien avance");
        Check.Near(pool[999].Life, 1.5, "et toutes ont vieilli");

        Check.Equal(Measure(() => UpdateAll(pool, 0.016f)), 0L,
            "mettre a jour 1000 particules dans un tableau reutilise : zero octet par frame");

        var objects = new List<ParticleObject>();

        Respawn(objects, 1000);

        Check.Equal(objects.Count, 1000, "la version objets marche aussi");

        long reused = Measure(() => UpdateAll(pool, 0.016f));
        long recreated = Measure(() => Respawn(objects, 1000));

        Check.Equal(reused, 0L, "d'un cote, rien");
        Check.True(recreated > 30000L,
            "de l'autre, 1000 allocations par frame : a 60 images par seconde ca fait 60 000 objets a ramasser");
        Check.True(recreated > reused, "c'est exactement la difference entre un jeu fluide et un jeu qui hoquette");
    }
}
