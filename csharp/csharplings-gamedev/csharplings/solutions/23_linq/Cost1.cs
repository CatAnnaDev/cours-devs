namespace Csharplings;

public sealed class Foe
{
    public string Name { get; set; }

    public int Health { get; set; }
}

public static class Cost1
{
    public const bool NotDone = false;

    private static readonly List<Foe> Foes = Build();

    private static List<Foe> Build()
    {
        var foes = new List<Foe>(200);

        for (int i = 0; i < 200; i++)
            foes.Add(new Foe { Name = "gobelin" + i, Health = i % 3 == 0 ? 0 : 10 });

        return foes;
    }

    public static long Measure(Action action)
    {
        action();
        action();
        action();

        long before = GC.GetAllocatedBytesForCurrentThread();
        action();

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    public static int CountAliveByHand()
    {
        int alive = 0;

        for (int i = 0; i < Foes.Count; i++)
        {
            if (Foes[i].Health > 0)
                alive++;
        }

        return alive;
    }

    public static int CountAliveByForeach()
    {
        int alive = 0;

        foreach (Foe foe in Foes)
        {
            if (foe.Health > 0)
                alive++;
        }

        return alive;
    }

    public static int CountAliveBoxed()
    {
        IEnumerable<Foe> foes = Foes;
        int alive = 0;

        foreach (Foe foe in foes)
        {
            if (foe.Health > 0)
                alive++;
        }

        return alive;
    }

    public static int CountAliveByLinq() => Foes.Count(foe => foe.Health > 0);

    public static int WeakestByHand()
    {
        Foe weakest = null;

        for (int i = 0; i < Foes.Count; i++)
        {
            if (weakest is null || Foes[i].Health < weakest.Health)
                weakest = Foes[i];
        }

        return weakest.Health;
    }

    public static int WeakestByLinq() => Foes.OrderBy(foe => foe.Health).First().Health;

    public static void Run()
    {
        Check.Equal(CountAliveByHand(), 133, "les quatre versions comptent la meme chose");
        Check.Equal(CountAliveByForeach(), 133, "a la boucle pres");
        Check.Equal(CountAliveBoxed(), 133, "et a l'interface pres");
        Check.Equal(CountAliveByLinq(), 133, "LINQ compris");

        Check.Equal(Measure(() => CountAliveByHand()), 0L, "une boucle for sur une List n'alloue rien");

        Check.Equal(Measure(() => CountAliveByForeach()), 0L,
            "un foreach non plus : l'enumerateur de List est un STRUCT, il vit sur la pile");

        long boxed = Measure(() => CountAliveBoxed());

        Check.True(boxed > 0L,
            $"mais range la meme liste dans un IEnumerable et le meme foreach alloue {boxed} octets : passer par l'interface EMBALLE l'enumerateur. Une signature qui prend IEnumerable<T> au lieu de List<T> suffit a payer ca, a chaque appel");

        Check.Equal(Measure(() => CountAliveByLinq()), 0L,
            "et pourtant Count avec un predicat ne coute RIEN ici : le compilateur voit que ni le delegue ni l'enumerateur ne sortent de la methode, et les pose sur la pile. C'est recent, et ce n'est vrai que sur un runtime recent : le meme code sur le Mono de Unity alloue");

        Check.Equal(Measure(() => { _ = Foes.Any(foe => foe.Health > 0); }), 0L,
            "meme chose pour Any, qui s'arrete en plus au premier trouve");

        long chained = Measure(() => { _ = Foes.Where(foe => foe.Health > 0).Select(foe => foe.Name).Count(); });

        Check.True(chained > 0L,
            $"des qu'on CHAINE, l'analyse ne tient plus : {chained} octets pour un Where suivi d'un Select, parce que chaque maillon fabrique un objet d'etat que le suivant garde");

        long materialized = Measure(() => { _ = Foes.Where(foe => foe.Health > 0).ToList(); });

        Check.True(materialized > chained * 5,
            $"et materialiser coute le vrai prix : {materialized} octets, la liste de sortie plus ses agrandissements successifs. Ce n'est pas du gaspillage, c'est le resultat qu'on a demande");

        Check.True(Measure(() => { _ = Foes.GroupBy(foe => foe.Health).Count(); }) > materialized,
            "GroupBy coute encore plus : une table de hachage, un groupe par cle, et chaque groupe est une liste");

        Check.Equal(Measure(() => { _ = Foes.Count; }), 0L,
            "la propriete Count d'une List, elle, est un champ : gratuite. Ce n'est pas la meme chose que la methode Count(), qui parcourt");

        Check.Equal(WeakestByHand(), 0, "les deux recherches du plus faible donnent le meme resultat");
        Check.Equal(WeakestByLinq(), 0, "au tri pres");

        Check.Equal(Measure(() => WeakestByHand()), 0L, "un seul passage, aucune allocation");

        long sorted = Measure(() => WeakestByLinq());
        long best = Measure(() => { _ = Foes.MinBy(foe => foe.Health); });

        Check.True(sorted > best,
            $"OrderBy().First() coute {sorted} octets contre {best} pour MinBy : demander un tri complet pour n'en garder qu'un reste plus cher, meme quand le runtime sait raccourcir");

        Check.True(materialized * 60 > 60_000L,
            $"la regle n'est pas 'LINQ est mauvais'. C'est {materialized} octets par appel, soit {materialized * 60} par seconde a 60 images. Au chargement, personne ne le verra. Dans l'Update, c'est une saccade toutes les quelques secondes");

        Check.True(Measure(() => { _ = Foes.Where(foe => foe.Health > 0).ToList().Count; })
                > Measure(() => { _ = Foes.Count(foe => foe.Health > 0); }),
            "et le reflexe qui coute le plus cher n'est pas LINQ : c'est materialiser une liste entiere pour ne lire que sa taille");
    }
}
