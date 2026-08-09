using System.Reflection;

namespace Csharplings;

public interface IGameSystem
{
    string Name { get; }
}

public sealed class MovementSystem : IGameSystem
{
    public string Name => "movement";
}

public sealed class CombatSystem : IGameSystem
{
    public string Name => "combat";
}

public abstract class HalfBakedSystem : IGameSystem
{
    public abstract string Name { get; }
}

public sealed class NeedsArgumentSystem : IGameSystem
{
    public NeedsArgumentSystem(int seed)
    {
        Seed = seed;
    }

    public int Seed { get; }

    public string Name => "seed" + Seed;
}

public static class Scan1
{
    public const bool NotDone = true;

    public static long Measure(Action action)
    {
        action();
        action();
        action();

        long before = GC.GetAllocatedBytesForCurrentThread();
        action();

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    public static List<Type> FindSystems() =>
        typeof(Scan1).Assembly
            .GetTypes()
            .Where(type => typeof(IGameSystem).IsAssignableFrom(type))
            .Where(type => !type.IsInterface)
            .ToList();

    public static List<IGameSystem> Instantiate(List<Type> types) =>
        types.Select(type => (IGameSystem)Activator.CreateInstance(type)).ToList();

    public static void Run()
    {
        List<Type> found = FindSystems();

        Check.Sequence(found.Select(type => type.Name), new[] { "CombatSystem", "MovementSystem" },
            "scanner l'assemblage trouve les implementations SANS que personne ne les ait declarees : c'est ce qui permet a un systeme ajoute dans un fichier d'exister sans toucher a une liste centrale");

        Check.False(found.Contains(typeof(HalfBakedSystem)),
            "une classe abstraite est ecartee : elle implemente l'interface mais on ne peut pas l'instancier");

        Check.False(found.Contains(typeof(IGameSystem)),
            "et l'interface elle-meme aussi, sans quoi le premier Activator planterait");

        Check.False(found.Contains(typeof(NeedsArgumentSystem)),
            "et celle qui n'a pas de constructeur sans argument. C'est LE filtre qu'on oublie : sans lui, ca marche jusqu'au jour ou un collegue ajoute un parametre a son constructeur, et le jeu ne demarre plus avec un MissingMethodException illisible");

        Check.Sequence(Instantiate(found).Select(system => system.Name), new[] { "combat", "movement" },
            "les instances sortent dans un ordre STABLE, parce qu'on a trie. L'ordre de GetTypes n'est garanti par rien : il peut changer d'une compilation a l'autre, et un jeu qui depend de cet ordre-la devient non reproductible");

        Check.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(NeedsArgumentSystem)),
            "voila l'erreur qu'on evite : elle ne dit pas quel type, elle ne dit pas quel constructeur, et elle sort au demarrage");

        Check.True(typeof(MovementSystem).IsAssignableTo(typeof(IGameSystem)), "IsAssignableTo se lit dans le sens naturel");
        Check.True(typeof(IGameSystem).IsAssignableFrom(typeof(MovementSystem)), "IsAssignableFrom dans l'autre : c'est la meme question posee a l'envers");

        Check.False(typeof(MovementSystem).IsAssignableFrom(typeof(IGameSystem)),
            "et les confondre est l'erreur la plus courante de tout code de reflexion : le test passe, la liste sort vide, et rien ne le signale");

        long scan = Measure(() => { _ = FindSystems(); });

        Check.True(scan > 500L,
            $"un scan complet coute {scan} octets ici, et lit les metadonnees de TOUS les types de l'assemblage. Sur un vrai jeu ce sont des milliers de types et des dizaines de millisecondes : c'est un prix de DEMARRAGE, on le paye une fois, on garde le resultat, et on ne rescanne jamais");

        Check.True(typeof(Scan1).Assembly.GetTypes().Length > found.Count * 2,
            "l'assemblage contient beaucoup plus de types que ceux qui nous interessent, et le scan les visite tous");
    }
}
