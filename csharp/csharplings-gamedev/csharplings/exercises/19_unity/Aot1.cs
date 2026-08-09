using Csharplings.Unity;

namespace Csharplings;

public interface ISpawnable
{
    string Describe();
}

public sealed class Slime : ISpawnable
{
    public string Describe() => "gluant";
}

public sealed class Bat : ISpawnable
{
    public string Describe() => "chauve-souris";
}

public sealed class Ghost : ISpawnable
{
    public string Describe() => "fantome";
}

public static class PerType<T>
{
    public static T Value;
}

public static class Aot1
{
    public const bool NotDone = true;

    private static readonly Dictionary<Type, object> Boxed = new();

    private static object _keepsItAlive;

    public static void Run()
    {
        Il2Cpp.Reset();

        Check.False(Il2Cpp.CanBuild(typeof(Slime)),
            "au depart, aucune ligne de code n'instancie un Slime : son constructeur n'a donc pas ete genere");

        Check.Throws<AotException>(() => Il2Cpp.BuildByReflection(typeof(Slime)),
            "et le construire par reflexion echoue. Pas a la compilation : A L'EXECUTION, et seulement sur la console, jamais dans l'editeur");

        Slime seen = Il2Cpp.Build<Slime>();

        Check.Equal(seen.Describe(), "gluant", "un 'new Slime()' ecrit noir sur blanc marche evidemment");
        Check.True(Il2Cpp.CanBuild(typeof(Slime)),
            "et il suffit a faire generer le constructeur : le compilateur a VU la construction");
        Check.True(Il2Cpp.BuildByReflection(typeof(Slime)) is Slime,
            "du coup la reflexion marche pour ce type-la. C'est ce qui rend le bug si vicieux : il ne se manifeste que pour les types qu'on ne construit nulle part");

        Check.Throws<AotException>(() => Il2Cpp.BuildByReflection(typeof(Bat)),
            "le voisin, lui, echoue toujours. Un editeur qui interprete ne voit pas la difference, une console compilee a l'avance si");

        Dictionary<string, Func<ISpawnable>> factory = BuildFactory();

        Check.Equal(factory["Slime"]().Describe(), "gluant", "une table de fabriques marche pour le premier");
        Check.Equal(factory["Bat"]().Describe(), "chauve-souris", "et pour le second");
        Check.True(Il2Cpp.CanBuild(typeof(Bat)),
            "parce qu'un '() => new Bat()' dans la table EST une construction visible : le compilateur genere, et rien n'est supprime");
        Check.False(factory.ContainsKey("Ghost"),
            "et un type absent de la table est absent tout court : l'erreur arrive a la construction de la table, pas trois heures plus tard sur console");

        Check.Equal(Il2Cpp.ReflectionCalls, 3, "trois tentatives par reflexion en tout, dont deux ont echoue");

        long byReflection = Report("une construction par reflexion", Allocations(() => _keepsItAlive = Il2Cpp.BuildByReflection(typeof(Slime))));
        long byTable = Report("une construction par la table", Allocations(() => _keepsItAlive = factory["Slime"]()));

        Check.Equal(byTable, byReflection,
            "et les deux coutent exactement la meme chose : les 24 octets de l'objet, rien de plus. Activator.CreateInstance est tres optimise en .NET moderne");
        Check.Equal(byTable, 24L,
            "la difference n'est donc NI la memoire NI la vitesse : c'est qu'une des deux peut tout simplement ne pas exister une fois compilee pour console. On choisit la table pour ca, pas pour des octets");

        Check.Equal(Report("mille ecritures dans un champ statique generique", Allocations(() => WritePerType(1000))), 0L,
            "un champ statique sur un type generique donne UN emplacement par type ferme : aucune recherche, aucun emballage, zero octet");

        long dictionary = Report("les memes mille ecritures dans un Dictionary<Type, object>", Allocations(() => WriteBoxed(1000)));

        Check.True(dictionary > 0L,
            "un registre indexe par Type doit emballer les types valeur : c'est le boxing de 15_perf, et en prime chaque cle demande une recherche");

        Check.Equal(PerType<int>.Value, 999, "le dernier ecrit est bien la");
        Check.Equal(PerType<float>.Value, 999f,
            "et PerType<int> et PerType<float> sont deux types FERMES differents, donc deux emplacements independants. Le compilateur les voit tous les deux, donc il les genere tous les deux");

        Il2Cpp.StripsUnreferencedCode = false;

        Check.True(Il2Cpp.BuildByReflection(typeof(Ghost)) is Ghost,
            "desactiver la suppression de code fait marcher la reflexion... au prix d'un binaire bien plus gros. C'est la solution de facilite");

        Il2Cpp.StripsUnreferencedCode = true;
        Il2Cpp.Preserve<Ghost>();

        Check.True(Il2Cpp.BuildByReflection(typeof(Ghost)) is Ghost,
            "la vraie porte de sortie est de marquer le type a conserver, un par un : c'est l'attribut Preserve et le fichier link.xml");
        Check.True(Il2Cpp.CanBuild(typeof(Ghost)),
            "mais une liste a tenir a la main se desynchronise toujours. La table de fabriques, elle, ne peut pas mentir : le compilateur la verifie");
    }

    private static Dictionary<string, Func<ISpawnable>> BuildFactory() =>
        new Dictionary<string, Func<ISpawnable>>
        {
            ["Slime"] = () => (ISpawnable)Il2Cpp.BuildByReflection(typeof(Slime)),
            ["Bat"] = () => (ISpawnable)Il2Cpp.BuildByReflection(typeof(Bat)),
        };

    private static void WritePerType(int rounds)
    {
        for (int i = 0; i < rounds; i++)
        {
            Boxed[typeof(int)] = i;
            Boxed[typeof(float)] = (float)i;
        }
    }

    private static void WriteBoxed(int rounds)
    {
        for (int i = 0; i < rounds; i++)
        {
            Boxed[typeof(int)] = i;
            Boxed[typeof(float)] = (float)i;
        }
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

    private static long Report(string what, long bytes)
    {
        Console.WriteLine($"      mesure  {what} : {bytes} octets");

        return bytes;
    }
}
