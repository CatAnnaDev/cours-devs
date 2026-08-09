namespace Csharplings;

public static class Strings2
{
    public const bool NotDone = true;

    public static string Normalize(string raw)
    {
        raw.Trim();
        raw.ToLower();
        return raw;
    }

    public static List<string> ParseTags(string raw)
    {
        return Todo.Value<List<string>>();
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
