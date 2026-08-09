namespace Csharplings;

public sealed class Enemy
{
    public Enemy(string name, int health, Vector2 position)
    {
        Name = name;
        Health = health;
        Position = position;
    }

    public string Name { get; }
    public int Health { get; }
    public Vector2 Position { get; }

    public void Deconstruct(out string name, out int health)
    {
        name = Name;
        health = Health;
    }
}

public static class Tuples1
{
    public const bool NotDone = false;

    public static (int Min, int Max) MinMax(List<int> values)
    {
        int min = values[0];
        int max = values[0];

        foreach (int value in values)
        {
            min = Mathf.Min(min, value);
            max = Mathf.Max(max, value);
        }

        return (min, max);
    }

    public static (Vector2 Position, Vector2 Velocity) Bounce(Vector2 position, Vector2 velocity, float floor)
    {
        Vector2 next = position + velocity;

        if (next.Y < floor)
            return (next, velocity);

        return (new Vector2(next.X, floor), new Vector2(velocity.X, -velocity.Y));
    }

    public static void Run()
    {
        (int min, int max) = MinMax(new List<int> { 40, 7, 25, 88, 12 });

        Check.Equal(min, 7, "le plus petit");
        Check.Equal(max, 88, "le plus grand");
        Check.Equal(MinMax(new List<int> { 5 }).Max, 5, "les champs d'un tuple peuvent porter un nom");

        var golem = new Enemy("golem", 200, new Vector2(10f, 4f));
        (string name, int health) = golem;

        Check.Equal(name, "golem", "Deconstruct destructure n'importe quelle classe");
        Check.Equal(health, 200, "en autant de morceaux qu'on veut");

        int a = 1;
        int b = 2;
        (a, b) = (b, a);

        Check.Equal(a, 2, "echanger deux variables sans variable temporaire");
        Check.Equal(b, 1, "la partie droite est evaluee en entier avant l'affectation");

        Check.True((1, "x") == (1, "x"), "deux tuples se comparent champ par champ");
        Check.False((1, "x") == (1, "y"), "et un seul champ suffit a les separer");

        (Vector2 flying, Vector2 speed) = Bounce(new Vector2(0f, 0f), new Vector2(1f, 2f), 100f);

        Check.Near(flying, new Vector2(1f, 2f), "en l'air on avance normalement");
        Check.Near(speed, new Vector2(1f, 2f), "et la vitesse ne change pas");

        (Vector2 landed, Vector2 bounced) = Bounce(new Vector2(0f, 90f), new Vector2(5f, 20f), 100f);

        Check.Near(landed, new Vector2(5f, 100f), "on ne traverse pas le sol");
        Check.Near(bounced, new Vector2(5f, -20f), "et on repart vers le haut");
    }
}
