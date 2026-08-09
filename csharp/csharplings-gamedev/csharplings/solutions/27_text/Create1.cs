using System.Globalization;

namespace Csharplings;

public static class Create1
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

    public static string HealthLabel(int current, int max) =>
        string.Create(null, stackalloc char[32], $"PV {current}/{max}");

    public static bool TryWriteHealth(Span<char> destination, int current, int max, out int written)
    {
        written = 0;

        if (!"PV ".TryCopyTo(destination))
            return false;

        written = 3;

        if (!current.TryFormat(destination.Slice(written), out int digits, provider: CultureInfo.InvariantCulture))
            return false;

        written += digits;

        if (written >= destination.Length)
            return false;

        destination[written++] = '/';

        if (!max.TryFormat(destination.Slice(written), out digits, provider: CultureInfo.InvariantCulture))
            return false;

        written += digits;

        return true;
    }

    public static string Repeat(char character, int count) =>
        string.Create(count, character, (span, value) => span.Fill(value));

    public static void Run()
    {
        Check.Equal(HealthLabel(37, 100), "PV 37/100", "string.Create compose dans un tampon puis fabrique la chaine finale");

        Check.Equal(HealthLabel(0, 0), "PV 0/0", "les cas limites passent par le meme chemin");

        Span<char> buffer = stackalloc char[32];

        Check.True(TryWriteHealth(buffer, 37, 100, out int written), "TryFormat ecrit dans un tampon FOURNI par l'appelant");
        Check.Equal(written, 9, "et dit combien de caracteres il a poses");
        Check.True(buffer.Slice(0, written).SequenceEqual("PV 37/100"), "avec le meme resultat");

        Check.Equal(Measure(() => { Span<char> scratch = stackalloc char[32]; TryWriteHealth(scratch, 37, 100, out _); }), 0L,
            "en ZERO octet : aucune chaine n'est fabriquee du tout. C'est ce qu'il faut quand le moteur sait consommer un span, et c'est le plancher absolu du texte en jeu");

        Check.True(Measure(() => { _ = HealthLabel(37, 100); }) > 0L,
            "string.Create alloue la chaine finale, et RIEN d'autre : pas de tampon intermediaire, pas de StringBuilder, pas de concatenation");

        Span<char> tiny = stackalloc char[4];

        Check.False(TryWriteHealth(tiny, 37, 100, out int partial),
            "un tampon trop petit rend false au lieu de lever : c'est la convention de tous les TryFormat du framework");

        Check.True(partial <= 4, "et ce qu'il a ecrit avant d'abandonner ne depasse jamais le tampon");

        Check.Equal(Repeat('-', 5), "-----", "string.Create prend un ETAT et un rappel qui remplit le tampon");

        Check.Equal(Repeat('#', 0), "", "une longueur nulle donne la chaine vide, sans allocation supplementaire");

        Check.Equal(Measure(() => { _ = Repeat('-', 5); }), Measure(() => { _ = new string('-', 5); }),
            "ici new string(char, int) fait la meme chose et se lit mieux : string.Create sert quand le contenu se CALCULE, pas quand il se repete");

        Span<char> numbers = stackalloc char[16];

        Check.True(1.5f.TryFormat(numbers, out int floatLength, "0.00", CultureInfo.InvariantCulture),
            "TryFormat existe sur tous les types numeriques, avec format et culture");
        Check.True(numbers.Slice(0, floatLength).SequenceEqual("1.50"), "et il respecte les deux");

        Check.True(DateTime.UnixEpoch.TryFormat(numbers, out int dateLength, "yyyy", CultureInfo.InvariantCulture),
            "sur les dates aussi");
        Check.True(numbers.Slice(0, dateLength).SequenceEqual("1970"), "meme resultat, zero allocation");
    }
}
