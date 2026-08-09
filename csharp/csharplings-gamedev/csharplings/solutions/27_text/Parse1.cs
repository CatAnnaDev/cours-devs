using System.Globalization;

namespace Csharplings;

public readonly record struct SpawnRule(string Kind, int Count, float Delay) : ISpanParsable<SpawnRule>
{
    public static SpawnRule Parse(ReadOnlySpan<char> text, IFormatProvider provider) =>
        TryParse(text, provider, out SpawnRule value) ? value : throw new FormatException($"regle illisible : {text}");

    public static bool TryParse(ReadOnlySpan<char> text, IFormatProvider provider, out SpawnRule result)
    {
        result = default;

        int first = text.IndexOf(':');

        if (first < 0)
            return false;

        ReadOnlySpan<char> rest = text.Slice(first + 1);
        int second = rest.IndexOf(':');

        if (second < 0)
            return false;

        if (!int.TryParse(rest.Slice(0, second), NumberStyles.Integer, provider, out int count))
            return false;

        if (!float.TryParse(rest.Slice(second + 1), NumberStyles.Float, provider, out float delay))
            return false;

        result = new SpawnRule(text.Slice(0, first).Trim().ToString(), count, delay);

        return true;
    }

    public static SpawnRule Parse(string text, IFormatProvider provider) => Parse(text.AsSpan(), provider);

    public static bool TryParse(string text, IFormatProvider provider, out SpawnRule result) =>
        TryParse(text.AsSpan(), provider, out result);
}

public static class Parse1
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

    public static int LoadRules(ReadOnlySpan<char> file, List<SpawnRule> destination)
    {
        destination.Clear();

        while (!file.IsEmpty)
        {
            int newline = file.IndexOf('\n');
            ReadOnlySpan<char> line = newline < 0 ? file : file.Slice(0, newline);

            if (SpawnRule.TryParse(line.Trim(), CultureInfo.InvariantCulture, out SpawnRule rule))
                destination.Add(rule);

            if (newline < 0)
                break;

            file = file.Slice(newline + 1);
        }

        return destination.Count;
    }

    public static void Run()
    {
        Check.True(SpawnRule.TryParse("gobelin:3:1.5", CultureInfo.InvariantCulture, out SpawnRule rule),
            "un type de donnees de jeu sait se lire lui-meme");
        Check.Equal(rule.Kind, "gobelin", "son nom");
        Check.Equal(rule.Count, 3, "son nombre");
        Check.Equal(rule.Delay, 1.5f, "et son delai");

        Check.False(SpawnRule.TryParse("gobelin:3", CultureInfo.InvariantCulture, out _), "une ligne incomplete est refusee");
        Check.False(SpawnRule.TryParse("gobelin:trois:1.5", CultureInfo.InvariantCulture, out _), "un nombre illisible aussi");
        Check.False(SpawnRule.TryParse("gobelin:3:vite", CultureInfo.InvariantCulture, out _),
            "et un delai illisible : CHAQUE conversion doit etre testee, sinon le champ garde sa valeur par defaut et la regle passe pour valide");
        Check.False(SpawnRule.TryParse("", CultureInfo.InvariantCulture, out _), "et une ligne vide");

        Check.Throws<FormatException>(() => SpawnRule.Parse("n'importe quoi", CultureInfo.InvariantCulture),
            "la version sans 'Try' leve, comme tous les Parse du framework : c'est la meme paire que int.Parse et int.TryParse, et elle vient de l'interface ISpanParsable");

        const string file = "gobelin:3:1.5\nslime:10:0.25\n\nabime:1:9\n";
        var rules = new List<SpawnRule>();

        Check.Equal(LoadRules(file, rules), 3, "trois regles lues, la ligne vide simplement ignoree");
        Check.Equal(rules[1].Kind, "slime", "la deuxieme");
        Check.Equal(rules[2].Delay, 9f, "et la troisieme, dont le delai est un entier");

        Check.True(Measure(() => LoadRules(file, rules)) < 500L,
            "et le fichier entier se lit en n'allouant QUE les noms : pas de tableau de lignes, pas de chaine par champ, pas de Substring. Sur une table de mille objets chargee au demarrage, c'est mille objets au lieu de dix mille");

        Check.True(int.TryParse("42".AsSpan(), out int number) && number == 42,
            "tous les types numeriques acceptent un span");

        Check.True(float.TryParse("1.5".AsSpan(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
            && Mathf.IsEqualApprox(value, 1.5f),
            "avec la culture explicite, sans quoi on retombe sur le probleme de culture1");

        Check.False(int.TryParse("42abc".AsSpan(), out _),
            "TryParse refuse ce qui n'est PAS entierement un nombre : il n'y a pas de lecture partielle, et c'est ce qui evite qu'un fichier corrompu passe a moitie");

        Check.False(int.TryParse(" 42 ".AsSpan(), NumberStyles.None, CultureInfo.InvariantCulture, out _),
            "et les espaces autour ne sont tolerees que si le style le dit : par defaut Integer les accepte, None non");

        Check.True(int.TryParse(" 42 ".AsSpan(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int spaced) && spaced == 42,
            "d'ou l'interet de choisir le style au lieu de le subir");
    }
}
