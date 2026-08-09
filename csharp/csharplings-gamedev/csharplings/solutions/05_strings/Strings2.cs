namespace Csharplings;

public static class Strings2
{
    public const bool NotDone = false;

    public static string Normalize(string raw)
    {
        return raw.Trim().ToLower();
    }

    public static List<string> ParseTags(string raw)
    {
        return raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(tag => tag.Trim())
            .ToList();
    }

    public static void Run()
    {
        Check.Equal(Normalize("  Slime  "), "slime", "une string est immuable : il faut recuperer le resultat");
        Check.Equal(Normalize("DRAGON"), "dragon", "tout en minuscules");

        Check.Sequence(ParseTags("feu,glace,vent"), new[] { "feu", "glace", "vent" }, "decouper sur les virgules");
        Check.Sequence(ParseTags("feu, glace , vent"), new[] { "feu", "glace", "vent" }, "sans les espaces autour");
        Check.Sequence(ParseTags(""), Array.Empty<string>(), "une chaine vide ne donne aucun tag");
    }
}
