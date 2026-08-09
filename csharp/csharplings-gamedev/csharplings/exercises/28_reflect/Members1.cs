using System.Reflection;

namespace Csharplings;

public sealed class Tunable
{
    public float Speed { get; set; } = 1f;

    public int Damage { get; set; } = 10;

    public string Label { get; set; } = "sans nom";

    public float Computed => Speed * Damage;

    private int Hidden { get; set; } = 7;
}

public static class Members1
{
    public const bool NotDone = true;

    private static readonly PropertyInfo[] Cached = typeof(Tunable)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .OrderBy(property => property.Name, StringComparer.Ordinal)
        .ToArray();

    public static long Measure(Action action)
    {
        action();
        action();
        action();

        long before = GC.GetAllocatedBytesForCurrentThread();
        action();

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    public static List<string> EditableNames() => Cached.Select(property => property.Name).ToList();

    public static bool TrySet(Tunable target, string name, object value)
    {
        foreach (PropertyInfo property in Cached)
        {
            if (!string.Equals(property.Name, name, StringComparison.Ordinal))
                continue;

            property.SetValue(target, value);

            return true;
        }

        return false;
    }

    public static Dictionary<string, object> Snapshot(Tunable target) =>
        Cached.ToDictionary(property => property.Name, property => property.GetValue(target));

    public static void Run()
    {
        Check.Sequence(EditableNames(), new[] { "Damage", "Label", "Speed" },
            "les proprietes publiques MODIFIABLES, triees : de quoi construire un panneau de reglages sans ecrire une ligne par champ");

        Check.False(EditableNames().Contains("Computed"),
            "une propriete calculee est ecartee par CanWrite : elle n'a pas de setter, et SetValue leverait");

        Check.False(EditableNames().Contains("Hidden"),
            "et une propriete privee n'apparait pas, parce que BindingFlags ne demande que le public");

        var tunable = new Tunable();

        Check.True(TrySet(tunable, "Speed", 2.5f), "modifier par nom");
        Check.Equal(tunable.Speed, 2.5f, "et la valeur arrive");

        Check.False(TrySet(tunable, "Vitesse", 1f), "un nom inconnu rend false");

        Check.False(TrySet(tunable, "Speed", "vite"),
            "et un TYPE incompatible aussi. Sans ce controle, SetValue leve une ArgumentException a l'execution, dans une pile d'appels de reflexion illisible");

        Check.Equal(tunable.Speed, 2.5f, "la valeur d'origine est intacte apres un refus");

        Dictionary<string, object> before = Snapshot(tunable);

        Check.Equal(before["Damage"], 10, "un instantane de tous les champs editables");
        Check.Equal(before.Count, 3, "trois champs");

        Check.True(Measure(() => { _ = tunable.Speed; }) == 0L, "lire une propriete directement ne coute rien");

        Check.True(Measure(() => { _ = Snapshot(tunable); }) > 0L,
            "GetValue, lui, EMBALLE la valeur : un float devient un objet sur le tas, a chaque lecture. C'est le prix de 'object' comme type de retour");

        Check.True(Measure(() => { _ = typeof(Tunable).GetProperties(); })
                > Measure(() => { _ = Cached.Length; }),
            "et retrouver les proprietes coute a chaque appel : GetProperties reconstruit son tableau, il ne le met pas en cache pour toi");

        Check.Equal(Cached.Length, 3,
            "d'ou la seule forme acceptable : chercher les membres UNE fois, dans un static readonly, et ne garder que les PropertyInfo. Tout ce qui suit n'est plus qu'un appel");

        Check.True(typeof(Tunable).GetProperty("Hidden", BindingFlags.NonPublic | BindingFlags.Instance) is not null,
            "la reflexion peut atteindre le prive quand on le demande explicitement : pratique pour un editeur, dangereux partout ailleurs, parce que plus rien ne garantit qu'un champ prive existera encore au prochain patch");
    }
}
