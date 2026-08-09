namespace Csharplings;

public struct Particle
{
    public float Life;
    public float Size;
}

public static class Refs1
{
    public const bool NotDone = true;

    public static void Heal(int health, int amount)
    {
        health = Mathf.Min(health + amount, 100);
    }

    public static bool TryFindIndex(List<int> values, int wanted, out int index)
    {
        for (int i = 0; i < values.Count; i++)
        {
            if (values[i] != wanted)
                continue;

            index = i;
            return true;
        }

        return false;
    }

    public static float Distance(in Vector2 from, in Vector2 to)
    {
        return from.DistanceTo(to);
    }

    public static int Slot(int[] inventory, int index)
    {
        return inventory[index];
    }

    public static Particle Oldest(Particle[] particles)
    {
        int best = 0;

        for (int i = 1; i < particles.Length; i++)
        {
            if (particles[i].Life < particles[best].Life)
                best = i;
        }

        return particles[best];
    }

    public static void Run()
    {
        int health = 50;
        Heal(ref health, 30);

        Check.Equal(health, 80, "'ref' donne l'adresse de la variable : la methode ecrit dedans");

        Heal(ref health, 500);

        Check.Equal(health, 100, "et le plafond s'applique quand meme");

        var values = new List<int> { 4, 8, 15 };

        Check.True(TryFindIndex(values, 8, out int found), "'out' sert de seconde valeur de retour");
        Check.Equal(found, 1, "l'index trouve");
        Check.False(TryFindIndex(values, 99, out int missing), "et le booleen dit si elle est valide");
        Check.Equal(missing, -1, "'out' DOIT etre assigne sur tous les chemins, meme celui de l'echec");

        Check.Near(Distance(new Vector2(0f, 0f), new Vector2(3f, 4f)), 5.0,
            "'in' passe l'adresse sans autoriser l'ecriture : pas de copie pour les grosses structures");

        var inventory = new[] { 1, 2, 3 };
        ref int slot = ref Slot(inventory, 1);
        slot = 99;

        Check.Sequence(inventory, new[] { 1, 99, 3 },
            "un 'ref int' n'est pas une copie de la case : c'est un ALIAS vers elle");

        int plain = Slot(inventory, 2);
        plain = 55;

        Check.Sequence(inventory, new[] { 1, 99, 3 },
            "sans 'ref' a la reception, on retombe sur une copie et on n'ecrit nulle part");

        var particles = new Particle[3];
        particles[0].Life = 5f;
        particles[1].Life = 1f;
        particles[2].Life = 9f;

        ref Particle dying = ref Oldest(particles);
        dying.Life = 0f;
        dying.Size = 42f;

        Check.Near(particles[1].Life, 0.0, "renvoyer 'ref' permet de modifier l'element du tableau en place");
        Check.Near(particles[1].Size, 42.0, "sans jamais recopier la structure");
        Check.Near(particles[0].Life, 5.0, "et sans toucher aux voisines");
    }
}
