namespace Csharplings;

public sealed class Wanderer : Node
{
    public Vector2 Velocity { get; set; } = new Vector2(60f, 0f);

    public void StepNaive(float delta)
    {
        Position = new Vector2(Position.X + Velocity.X * delta, Position.Y + Velocity.Y * delta);

        if (Position.X > 100f)
            Position = new Vector2(0f, Position.Y);
    }

    public void StepBatched(float delta)
    {
        Position += Velocity * delta;

        if (Position.X > 100f)
            Position = new Vector2(0f, Position.Y);
    }
}

public static class Crossing1
{
    public const bool NotDone = true;

    public static void Nudge(Node node, float amount)
    {
        node.Position.X += amount;
    }

    public static void Run()
    {
        var naive = new Wanderer { Name = "Naif" };
        var batched = new Wanderer { Name = "Groupe" };

        Node.PropertyCrossings = 0;
        naive.StepNaive(1f / 60f);
        int naiveCrossings = Report("un pas ecrit naivement", Node.PropertyCrossings);

        Node.PropertyCrossings = 0;
        batched.StepBatched(1f / 60f);
        int batchedCrossings = Report("le meme pas, lu et ecrit une seule fois", Node.PropertyCrossings);

        Check.Near(naive.Position, batched.Position,
            "les deux versions donnent exactement la meme position : c'est bien le meme calcul");

        Check.Equal(naiveCrossings, 4,
            "mais la version naive franchit QUATRE fois la frontiere C# vers moteur pour un seul deplacement. Chaque lecture de Position est un appel natif, pas un acces a un champ");
        Check.Equal(batchedCrossings, 2,
            "la version groupee : une lecture, une ecriture. Deux. Et c'est le meme code, en trois lignes de plus");

        Node.PropertyCrossings = 0;
        Nudge(naive, 5f);

        Check.Equal(Node.PropertyCrossings, 2,
            "pour modifier un seul axe il faut lire, fabriquer un nouveau vecteur, reecrire : une propriete rend une COPIE, on ne peut pas ecrire dans un de ses champs");
        Check.Near(naive.Position.X, 6.0, "et la valeur est bien montee de cinq", 0.001);

        var crowd = new List<Wanderer>(1000);

        for (int i = 0; i < 1000; i++)
            crowd.Add(new Wanderer());

        Node.PropertyCrossings = 0;

        for (int frame = 0; frame < 60; frame++)
        {
            for (int i = 0; i < crowd.Count; i++)
                crowd[i].StepNaive(1f / 60f);
        }

        int crowdNaive = Report("1000 objets sur 60 frames, version naive", Node.PropertyCrossings);

        Node.PropertyCrossings = 0;

        for (int frame = 0; frame < 60; frame++)
        {
            for (int i = 0; i < crowd.Count; i++)
                crowd[i].StepBatched(1f / 60f);
        }

        int crowdBatched = Report("les memes, version groupee", Node.PropertyCrossings);

        Check.Equal(crowdNaive, 240_000, "une seconde de jeu : deux cent quarante mille appels natifs");
        Check.Equal(crowdBatched, 120_000, "contre cent vingt mille. Cent vingt mille appels economises par SECONDE de jeu");
        Check.Equal(crowdNaive / crowdBatched, 2, "deux fois moins, sans changer une virgule au comportement");

        Check.Equal(Allocations(() => batched.StepBatched(1f / 60f)), 0L,
            "et rien de tout ca n'alloue : ce cout n'apparaitra jamais dans un profil de ramasse-miettes. C'est pour ca qu'on ne le trouve pas");
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

    private static int Report(string what, int crossings)
    {
        Console.WriteLine($"      mesure  {what} : {crossings} franchissements");

        return crossings;
    }
}
