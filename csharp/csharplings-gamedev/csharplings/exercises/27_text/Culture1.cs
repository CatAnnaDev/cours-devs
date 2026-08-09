using System.Globalization;

namespace Csharplings;

public static class Culture1
{
    public const bool NotDone = true;

    public static readonly NumberFormatInfo Comma = new() { NumberDecimalSeparator = ",", NumberGroupSeparator = " " };

    public static string WriteSetting(float value) => value.ToString("R", Comma);

    public static bool TryReadSetting(string text, out float value) =>
        float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    public static string ShowToPlayer(float value, NumberFormatInfo format) => value.ToString("N2", format);

    public static Dictionary<string, int> BuildIndex() => new(StringComparer.OrdinalIgnoreCase);

    public static void Run()
    {
        Check.Equal(WriteSetting(1.5f), "1.5", "un reglage s'ecrit TOUJOURS en culture invariante : un point, jamais autre chose");

        Check.True(TryReadSetting("1.5", out float back) && Mathf.IsEqualApprox(back, 1.5f), "et se relit pareil");

        Check.Equal(1.5f.ToString(Comma), "1,5",
            "sur une machine francaise, la culture par defaut ecrit une VIRGULE. Le fichier de sauvegarde devient illisible pour tous les autres joueurs, et pour le meme joueur qui change la langue de son systeme");

        Check.False(TryReadSetting("1,5", out float broken),
            "relu en invariant, ce nombre-la echoue franchement");

        Check.Equal(broken, 0f, "et rend zero : un reglage de sensibilite a zero, une position a l'origine, un volume muet");

        Check.Equal(ShowToPlayer(1234.5f, Comma), "1 234,50",
            "AFFICHER au joueur, c'est l'inverse : la, il faut sa culture a lui, groupes de milliers compris");

        Check.Equal(ShowToPlayer(1234.5f, CultureInfo.InvariantCulture.NumberFormat), "1,234.50",
            "la regle tient en une phrase : culture invariante pour ce que la MACHINE relit, culture du joueur pour ce que l'HUMAIN lit");

        Check.True(string.Equals("Epee", "epee", StringComparison.OrdinalIgnoreCase),
            "pour comparer des identifiants, on compare des OCTETS : Ordinal et OrdinalIgnoreCase");

        Check.False(string.Equals("Epee", "epee", StringComparison.Ordinal), "la version sensible a la casse ne pardonne rien");

        Check.Equal("epee".ToUpperInvariant(), "EPEE",
            "et on met en majuscules en INVARIANT. En turc, ToUpper transforme le i en I point suspendu, ce qui casse toute cle de dictionnaire qui contient la lettre i - le bug le plus celebre de la localisation");

        Dictionary<string, int> index = BuildIndex();

        index["Epee"] = 1;
        index["epee"] = 2;

        Check.Equal(index.Count, 2,
            "un dictionnaire ORDINAL distingue les deux : c'est ce qu'on veut pour des cles techniques, et c'est aussi la version la plus rapide, parce qu'elle ne consulte aucune table de langue");

        var loose = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["Epee"] = 1 };

        Check.True(loose.ContainsKey("EPEE"), "et la version insensible a la casse sert pour ce que l'utilisateur tape");

        Check.Equal(string.Compare("a", "B", StringComparison.Ordinal) > 0, true,
            "attention au tri : en ordinal, 'B' passe AVANT 'a', parce qu'on compare des codes. Pour une liste montree au joueur, il faut un tri culturel, sinon les majuscules se regroupent en tete");

        Check.True(CultureInfo.InvariantCulture.Name.Length == 0,
            "dernier point, et c'est un choix de production : ce runner tourne en mode globalisation invariante, comme beaucoup de jeux exportes. Toutes les cultures s'y comportent comme l'invariante, l'executable est plus petit, et rien ne depend de la machine du joueur");
    }
}
