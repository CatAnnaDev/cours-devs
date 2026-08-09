using System.Globalization;
using System.Text;

namespace Csharplings;

public static class Format1
{
    public const bool NotDone = false;

    public static readonly CompositeFormat Damage =
        CompositeFormat.Parse("{0} inflige {1:0.0} degats a {2}");

    public static long Measure(Action action)
    {
        action();
        action();
        action();

        long before = GC.GetAllocatedBytesForCurrentThread();
        action();

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    public static string DamageLine(string attacker, float amount, string target) =>
        string.Format(CultureInfo.InvariantCulture, Damage, attacker, amount, target);

    public static string Column(string name, int score) =>
        string.Format(CultureInfo.InvariantCulture, "{0,-10}{1,6}", name, score);

    public static void Run()
    {
        Check.Equal(1.5f.ToString("0.00", CultureInfo.InvariantCulture), "1.50",
            "un format personnalise decrit la forme voulue chiffre par chiffre");

        Check.Equal(1234.5678f.ToString("0.##", CultureInfo.InvariantCulture), "1234.57",
            "le diese est un chiffre FACULTATIF : il arrondit sans imposer de zero");

        Check.Equal(1.5f.ToString("0.##", CultureInfo.InvariantCulture), "1.5", "d'ou la difference avec le zero, qui force la decimale");

        Check.Equal(42.ToString("D5", CultureInfo.InvariantCulture), "00042",
            "les formats standard tiennent en une lettre : D pour un entier complete de zeros");

        Check.Equal(0.75f.ToString("P0", CultureInfo.InvariantCulture), "75 %", "P pour un pourcentage, multiplication comprise");
        Check.Equal(255.ToString("X2", CultureInfo.InvariantCulture), "FF", "X pour de l'hexadecimal, indispensable pour une couleur");
        Check.Equal(1234567.ToString("N0", CultureInfo.InvariantCulture), "1,234,567", "N pour des groupes de milliers");

        Check.Equal(Column("anna", 30), "anna          30",
            "la virgule dans une expression de format ALIGNE : negatif a gauche, positif a droite. C'est ce qui fait un tableau de scores lisible en police a chasse fixe");

        Check.Equal(Column("bartholomew", 5), "bartholomew     5",
            "et une valeur plus longue que sa colonne n'est jamais tronquee : l'alignement est un minimum, pas un maximum");

        Check.Equal(DamageLine("anna", 12.34f, "gobelin"), "anna inflige 12.3 degats a gobelin",
            "un CompositeFormat analyse le gabarit UNE fois, a la construction");

        Check.Equal(DamageLine("bob", 5f, "slime"), "bob inflige 5.0 degats a slime", "puis se reutilise a chaque appel");

        long composite = Measure(() => { _ = DamageLine("anna", 12.34f, "gobelin"); });
        long raw = Measure(() => { _ = string.Format(CultureInfo.InvariantCulture, "{0} inflige {1:0.0} degats a {2}", "anna", 12.34f, "gobelin"); });

        Check.True(composite <= raw,
            $"{composite} octets contre {raw} : le gain n'est pas la, il est en TEMPS. La version ordinaire relit le gabarit caractere par caractere a chaque appel pour retrouver ses accolades ; le CompositeFormat l'a fait une fois. Pour un gabarit traduit, charge au demarrage et utilise mille fois, c'est exactement le cas d'usage");

        Check.True(Measure(() => { _ = $"anna inflige {12.34f:0.0} degats a gobelin"; }) <= raw,
            "une chaine interpolee ordinaire est deja mieux que string.Format : le compilateur la traduit en appels directs, sans tableau d'arguments ni emballage");

        Check.Throws<FormatException>(() => CompositeFormat.Parse("{0} et {"),
            "et un gabarit invalide echoue A LA CONSTRUCTION, pas au milieu d'un combat trois heures plus tard. C'est tout l'interet de l'analyser une fois");

        Check.Equal(string.Format(CultureInfo.InvariantCulture, "{{litteral}} {0}", 1), "{litteral} 1",
            "une accolade se double pour l'ecrire telle quelle");

        Check.Equal($"{new Vector2(1.5f, -2f)}", "(1.5, -2)",
            "et un type a soi decide de son texte avec ToString : c'est ce que l'interpolation appelle, pour lui comme pour le reste");
    }
}
