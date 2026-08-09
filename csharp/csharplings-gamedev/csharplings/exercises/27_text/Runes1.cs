using System.Text;

namespace Csharplings;

public static class Runes1
{
    public const bool NotDone = true;

    public const string Gamepad = "\U0001F3AE";

    public static int RuneCount(string text) => text.Length;

    public static string TruncateByChars(string text, int max) =>
        text.Length <= max ? text : text.Substring(0, max);

    public static string TruncateByRunes(string text, int max)
    {
        if (text.Length <= max)
            return text;

        int used = 0;

        foreach (Rune rune in text.EnumerateRunes())
        {
            if (used + 1 > max)
                break;

            used++;
        }

        return text.Substring(0, used);
    }

    public static void Run()
    {
        Check.Equal(Gamepad.Length, 2,
            "un emoji de manette a une LONGUEUR de deux. Un char n'est pas un caractere : c'est une unite UTF-16 de seize bits, et tout ce qui depasse s'ecrit sur DEUX unites");

        Check.Equal(RuneCount(Gamepad), 1, "une Rune, elle, est un vrai point de code : il y en a un seul");

        Check.True(char.IsHighSurrogate(Gamepad[0]), "le premier char est un demi-caractere haut");
        Check.True(char.IsLowSurrogate(Gamepad[1]), "le second un demi-caractere bas");
        Check.False(char.IsLetter(Gamepad[0]), "et pris tout seul, aucun des deux ne veut rien dire");

        string pseudo = "anna" + Gamepad + "bob";

        Check.Equal(pseudo.Length, 9, "quatre plus deux plus trois");
        Check.Equal(RuneCount(pseudo), 8, "mais huit caracteres pour un humain");

        Check.Equal(TruncateByChars(pseudo, 5), "anna\ud83c",
            "tronquer a l'aveugle coupe l'emoji EN DEUX, et rend une chaine qui contient un demi-caractere. C'est le carre blanc dans les pseudos, et parfois un plantage du moteur de rendu");

        Check.Equal(TruncateByRunes(pseudo, 5), "anna",
            "tronquer par RUNES s'arrete avant : mieux vaut un caractere de moins qu'un caractere casse");

        Check.Equal(TruncateByRunes(pseudo, 6), "anna" + Gamepad, "et quand la place suffit, l'emoji passe entier");

        Check.Equal(TruncateByRunes("anna", 10), "anna", "une chaine plus courte que la limite est rendue telle quelle");

        Check.Equal(Encoding.UTF8.GetByteCount(Gamepad), 4,
            "en UTF-8, le meme emoji pese QUATRE octets : trois comptes differents pour un seul caractere, et il faut savoir lequel une API attend");

        Check.Equal(Encoding.UTF8.GetByteCount("anna"), 4, "les lettres latines, elles, font un octet chacune");
        Check.Equal(Encoding.UTF8.GetByteCount("e"), 1, "un octet");

        Check.True(Rune.TryCreate(Gamepad[0], Gamepad[1], out Rune joined), "deux demi-caracteres se recombinent en une rune");
        Check.Equal(joined.Value, 0x1F3AE, "avec son vrai point de code");
        Check.Equal(joined.Utf16SequenceLength, 2, "qui occupe deux unites UTF-16");
        Check.Equal(joined.Utf8SequenceLength, 4, "et quatre octets UTF-8");

        Check.False(Rune.TryCreate(Gamepad[0], out _),
            "et un demi-caractere seul ne fait PAS une rune : c'est exactement ce que la troncature naive vient de fabriquer");

        Check.Equal(new Rune('a').Value, 97, "les caracteres ordinaires deviennent des runes sans histoire");

        Check.Equal(RuneCount("naive"), 5,
            "derniere chose : meme un compte de runes juste ne fait pas tout. Une lettre accentuee peut s'ecrire en DEUX runes, la lettre puis l'accent, et un drapeau en deux runes aussi. Pour ce que l'oeil percoit comme un caractere, il faut compter les GRAPHEMES");
    }
}
