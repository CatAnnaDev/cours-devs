using System.Linq.Expressions;
using System.Reflection;

namespace Csharplings;

public sealed class Projectile
{
    public int Bounces { get; set; }
}

public abstract class AbstractProjectile
{
}

public static class Activator1
{
    public const bool NotDone = false;

    public const int Calls = 1000;

    public static int Reflections;

    public static readonly Dictionary<string, Func<Projectile>> Table = new(StringComparer.Ordinal)
    {
        ["projectile"] = static () => new Projectile(),
    };

    public static Projectile CreateByReflection()
    {
        Reflections++;

        return (Projectile)Activator.CreateInstance(typeof(Projectile));
    }

    public static Func<Projectile> CompiledFactory()
    {
        Reflections++;

        ConstructorInfo constructor = typeof(Projectile).GetConstructor(Type.EmptyTypes);

        return Expression.Lambda<Func<Projectile>>(Expression.New(constructor)).Compile();
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

    public static int Spawn(Func<Projectile> make)
    {
        int total = 0;

        for (int i = 0; i < Calls; i++)
            total += make().Bounces + 1;

        return total;
    }

    public static void Run()
    {
        Check.Equal(new Projectile().Bounces, 0, "un 'new' ordinaire");

        Reflections = 0;

        Check.Equal(CreateByReflection().Bounces, 0,
            "Activator.CreateInstance fabrique le meme objet a partir d'un Type connu seulement a l'execution");

        Check.Equal(Reflections, 1, "en passant par la reflexion, une fois");

        Reflections = 0;

        Check.Equal(Spawn(() => CreateByReflection()), Calls, "mille projectiles fabriques par reflexion");

        Check.Equal(Reflections, Calls,
            "et MILLE passages par la reflexion : Activator recherche le constructeur, verifie les droits et emballe le resultat en object a chaque appel. Rien de ce travail n'est reutilise d'un appel sur l'autre");

        Reflections = 0;

        Func<Projectile> compiled = CompiledFactory();

        Check.Equal(Spawn(compiled), Calls, "la fabrique compilee fabrique les memes mille projectiles");

        Check.Equal(Reflections, 1,
            "avec UN seul passage par la reflexion, a la construction. L'arbre d'expression est traduit en code machine une fois, et les mille appels suivants sont des appels de delegue ordinaires");

        Reflections = 0;

        Check.Equal(Spawn(Table["projectile"]), Calls, "la table de lambdas aussi");

        Check.Equal(Reflections, 0,
            "avec ZERO reflexion : il n'y a rien a chercher, la lambda contient deja le 'new'");

        Check.Equal(Measure(() => { _ = Table["projectile"](); }), Measure(() => { _ = new Projectile(); }),
            "et elle coute exactement ce que coute le 'new' qu'elle contient, pas un octet de plus");

        Check.True(Measure(() => { _ = CreateByReflection(); }) >= Measure(() => { _ = new Projectile(); }),
            "la reflexion, elle, alloue au moins autant, et souvent plus");

        Check.Equal(Table.Count, 1, "la table a un dernier avantage, et c'est le principal");

        Check.True(Table.ContainsKey("projectile"),
            "le compilateur VOIT chaque 'new' qu'elle contient. Donc le trim ne les supprime pas, IL2CPP genere leur code, et un type oublie se voit a la lecture de la table - pas trois heures plus tard sur une console");

        Check.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(AbstractProjectile)),
            "et ce que la reflexion ne verifie qu'a l'EXECUTION - qu'un type est instanciable - une table le rend impossible a ecrire : 'new AbstractProjectile()' ne compile pas");
    }
}
