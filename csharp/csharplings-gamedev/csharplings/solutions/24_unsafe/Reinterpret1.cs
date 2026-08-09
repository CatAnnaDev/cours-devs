using System.Runtime.InteropServices;

namespace Csharplings;

public static class Reinterpret1
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

    public static ReadOnlySpan<float> AsFloats(ReadOnlySpan<Vector2> points) =>
        MemoryMarshal.Cast<Vector2, float>(points);

    public static ReadOnlySpan<byte> AsBytes(ReadOnlySpan<Vector2> points) =>
        MemoryMarshal.AsBytes(points);

    public static Vector2 ReadPoint(ReadOnlySpan<byte> raw, int offset) =>
        MemoryMarshal.Read<Vector2>(raw.Slice(offset));

    public static void WritePoint(Span<byte> raw, int offset, Vector2 point) =>
        MemoryMarshal.Write(raw.Slice(offset), in point);

    public static int CountFloats(ReadOnlySpan<Vector2> points)
    {
        int count = 0;

        foreach (float component in MemoryMarshal.Cast<Vector2, float>(points))
        {
            if (component != 0f)
                count++;
        }

        return count;
    }

    public static void Run()
    {
        var points = new[] { new Vector2(1f, 2f), new Vector2(3f, 4f) };

        ReadOnlySpan<float> floats = AsFloats(points);

        Check.Equal(floats.Length, 4, "deux Vector2 vus comme quatre floats : la vue change, la memoire non");
        Check.Equal(floats[0], 1f, "les composantes se suivent en memoire, x puis y");
        Check.Equal(floats[3], 4f, "et le dernier float est le y du dernier point");

        Check.Equal(Measure(() => { _ = AsFloats(points).Length; }), 0L,
            "sans UNE seule copie : c'est exactement ce qu'attend une API graphique ou un envoi reseau qui veut un tableau de floats");

        Check.Equal(AsBytes(points).Length, 16, "vus comme des octets, ils en font seize : quatre floats de quatre octets");

        Check.Equal(CountFloats(new[] { new Vector2(1f, 0f), new Vector2(0f, 0f) }), 1,
            "et on peut travailler composante par composante sans deballer les structs un a un");

        var raw = new byte[16];

        WritePoint(raw, 0, new Vector2(5f, 6f));
        WritePoint(raw, 8, new Vector2(7f, 8f));

        Check.Near(ReadPoint(raw, 0), new Vector2(5f, 6f), "ecrire un struct dans un tampon d'octets");
        Check.Near(ReadPoint(raw, 8), new Vector2(7f, 8f), "et le relire a la bonne position : la sauvegarde binaire tient en deux lignes");

        Check.Equal(raw.Length / 8, 2, "huit octets par point, deux points");

        Span<Vector2> back = MemoryMarshal.Cast<byte, Vector2>(raw.AsSpan());

        Check.Equal(back.Length, 2, "et le chemin inverse marche aussi");
        Check.Near(back[1], new Vector2(7f, 8f), "les memes octets, relus comme des structs");

        back[1] = new Vector2(9f, 9f);

        Check.Near(ReadPoint(raw, 8), new Vector2(9f, 9f),
            "une vue n'est pas une copie : ecrire dedans ecrit dans le tableau d'octets d'origine");

        var odd = new byte[10];

        Check.Equal(MemoryMarshal.Cast<byte, Vector2>(odd).Length, 1,
            "un reste qui ne fait pas un element entier est simplement ignore : la longueur est TRONQUEE, jamais arrondie vers le haut");

        Check.Throws<ArgumentOutOfRangeException>(() => MemoryMarshal.Read<Vector2>(new byte[4]),
            "et lire un struct dans un tampon trop court echoue franchement au lieu de lire ce qui traine derriere");
    }
}
