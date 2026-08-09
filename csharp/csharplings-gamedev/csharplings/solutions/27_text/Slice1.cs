namespace Csharplings;

public static class Slice1
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

    public static int CountFields(ReadOnlySpan<char> line, char separator)
    {
        if (line.IsEmpty)
            return 0;

        int count = 1;

        foreach (char character in line)
        {
            if (character == separator)
                count++;
        }

        return count;
    }

    public static int SumNumbers(ReadOnlySpan<char> line, char separator)
    {
        int total = 0;

        while (true)
        {
            int cut = line.IndexOf(separator);
            ReadOnlySpan<char> field = cut < 0 ? line : line.Slice(0, cut);

            if (int.TryParse(field.Trim(), out int value))
                total += value;

            if (cut < 0)
                return total;

            line = line.Slice(cut + 1);
        }
    }

    public static int MajorVersion(ReadOnlySpan<char> version)
    {
        int dot = version.IndexOf('.');

        return int.Parse(dot < 0 ? version : version.Slice(0, dot));
    }

    public static bool IsCommand(ReadOnlySpan<char> line, ReadOnlySpan<char> name) =>
        line.StartsWith(name, StringComparison.Ordinal)
            && (line.Length == name.Length || line[name.Length] == ' ');

    public static void Run()
    {
        const string line = "12, 30, 7, 51";

        Check.Equal(CountFields(line, ','), 4, "quatre champs");
        Check.Equal(SumNumbers(line, ','), 100, "et leur somme");

        Check.Equal(Measure(() => { _ = SumNumbers(line, ','); }), 0L,
            "en ZERO octet. Un ReadOnlySpan<char> est une FENETRE sur la chaine d'origine : le decouper ne copie rien");

        Check.True(Measure(() => { _ = line.Split(',').Sum(part => int.Parse(part.Trim())); }) > 0L,
            "la meme chose avec Split alloue un tableau, plus une chaine par champ, plus une chaine par Trim. Sur un fichier de donnees de mille lignes, ce sont des milliers d'objets pour un resultat de quatre octets");

        Check.Equal(MajorVersion("4.2.1"), 4, "int.Parse accepte directement un span : plus besoin de Substring");
        Check.Equal(MajorVersion("12"), 12, "et le cas sans separateur passe par le meme chemin");

        Check.Equal(Measure(() => { _ = MajorVersion("4.2.1"); }), 0L, "sans allouer non plus");

        Check.Equal(SumNumbers("", ','), 0, "une ligne vide donne zero");
        Check.Equal(CountFields("", ','), 0, "et zero champ, pas un");
        Check.Equal(CountFields("seul", ','), 1, "un champ sans separateur reste un champ");

        Check.Equal(SumNumbers("12,,7", ','), 19, "un champ vide est simplement ignore par TryParse");
        Check.Equal(SumNumbers("12,abc,7", ','), 19, "et un champ illisible aussi : c'est ce qui rend un chargement de donnees tolerant");

        Check.True(IsCommand("spawn gobelin 3", "spawn"), "reconnaitre une commande par son prefixe");
        Check.True(IsCommand("spawn", "spawn"), "y compris toute seule");
        Check.False(IsCommand("spawner 3", "spawn"),
            "et surtout PAS 'spawner' : un StartsWith tout nu attrape tous les mots qui commencent pareil, et c'est le bug de console de commande le plus classique");

        ReadOnlySpan<char> padded = "   epee   ";

        Check.Equal(padded.Trim().Length, 4, "Trim sur un span deplace les bornes de la fenetre");
        Check.Equal(padded.TrimStart().TrimEnd().Length, 4, "sans jamais toucher a la chaine d'origine");

        Check.Equal("epee de feu".AsSpan().LastIndexOf(' '), 7, "toute la famille IndexOf existe sur les spans");

        Check.True("epee".AsSpan().SequenceEqual("epee"),
            "et la comparaison se fait avec SequenceEqual : '==' sur deux spans comparerait les fenetres, pas leur contenu");
    }
}
