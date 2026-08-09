using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Csharplings;

public interface IEffect
{
    string Apply();
}

public sealed class BurnEffect : IEffect
{
    public string Apply() => "brule";
}

public sealed class FreezeEffect : IEffect
{
    public string Apply() => "gele";
}

public static class EffectsByReflection
{
    public static IEffect Create(string typeName)
    {
        Type type = Type.GetType(typeName);

        return type is null ? null : (IEffect)Activator.CreateInstance(type);
    }
}

public static class EffectsByTable
{
    public static readonly Dictionary<string, Func<IEffect>> Factories = new(StringComparer.Ordinal)
    {
        ["burn"] = static () => (IEffect)Activator.CreateInstance(Type.GetType("Csharplings.BurnEffect")),
        ["freeze"] = static () => (IEffect)Activator.CreateInstance(Type.GetType("Csharplings.FreezeEffect")),
    };

    public static IEffect Create(string key) =>
        Factories.TryGetValue(key, out Func<IEffect> factory) ? factory() : null;
}

public static class Trim1
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

    public static void Run()
    {
        Check.Equal(EffectsByReflection.Create("Csharplings.BurnEffect").Apply(), "brule",
            "la version par reflexion marche ici, dans l'editeur, sur ta machine");

        Check.True(EffectsByReflection.Create("Csharplings.Inexistant") is null,
            "et rend null pour un nom inconnu, ce qui est deja mieux que de lever");

        Check.Equal(EffectsByTable.Create("burn").Apply(), "brule", "la table donne le meme resultat");
        Check.Equal(EffectsByTable.Create("freeze").Apply(), "gele", "pour chaque effet");
        Check.True(EffectsByTable.Create("inconnu") is null, "et le meme null pour un inconnu");

        Check.Equal(EffectsByTable.Factories.Count, 2,
            "mais la table contient des 'new' que le COMPILATEUR VOIT. Un nom de type dans une chaine, non : rien ne relie 'Csharplings.BurnEffect' a la classe BurnEffect");

        Check.True(Measure(() => { _ = EffectsByTable.Create("burn"); })
                < Measure(() => { _ = EffectsByReflection.Create("Csharplings.BurnEffect"); }),
            "cote cout, la table gagne aussi : Type.GetType analyse la chaine et fouille les assemblages a chaque appel");

        MethodInfo risky = typeof(EffectsByReflection).GetMethod(nameof(EffectsByReflection.Create));

        Check.True(risky.GetCustomAttribute<RequiresUnreferencedCodeAttribute>() is not null,
            "RequiresUnreferencedCode MARQUE le code incompatible avec le trim : l'analyseur previent alors a chaque appel, au lieu de laisser la surprise pour la version console");

        Check.True(risky.GetCustomAttribute<RequiresUnreferencedCodeAttribute>().Message.Contains("trim"),
            "et le message explique quoi faire, parce que celui qui le lira ne sera pas celui qui l'a ecrit");

        Type kept = typeof(BurnEffect);

        Check.Equal(kept.Name, "BurnEffect",
            "un typeof, lui, est une REFERENCE que le trim comprend : le type est conserve, avec ses membres utilises");

        Check.True(typeof(IEffect).IsAssignableFrom(kept), "et l'interface aussi, puisqu'on s'en sert");

        Check.Equal(EffectsByTable.Factories.Keys.Order().ToList().Count, 2,
            "la regle de la section, et c'est la meme que dans 19_unity/aot1 : ce que le compilateur ne VOIT pas n'existe pas. Le trim le supprime, IL2CPP ne genere pas son code, et l'echec sort a l'execution, sur la machine du joueur, pas dans ton editeur");

        Check.True(EffectsByTable.Factories.ContainsKey("burn"),
            "un scan par reflexion au DEMARRAGE reste possible - il faut juste que la table qu'il remplit soit ecrite en dur quelque part, ou que les types soient preserves explicitement");
    }
}
