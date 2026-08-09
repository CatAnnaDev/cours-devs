using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;

namespace Csharplings;

public ref struct BlobWriter
{
    private readonly Span<byte> _destination;

    public BlobWriter(Span<byte> destination)
    {
        _destination = destination;
        Written = 0;
    }

    public int Written { get; private set; }

    public void WriteInt(int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(Take(4), value);
    }

    public void WriteFloat(float value)
    {
        BinaryPrimitives.WriteSingleLittleEndian(Take(4), value);
    }

    public void WriteVector(Vector2 value)
    {
        WriteFloat(value.X);
        WriteFloat(value.Y);
    }

    public void WriteText(string value)
    {
        int length = Encoding.UTF8.GetByteCount(value);

        WriteInt(length);
        Encoding.UTF8.GetBytes(value, Take(length));
    }

    private Span<byte> Take(int count)
    {
        Span<byte> slice = _destination.Slice(Written, count);

        Written += count;

        return slice;
    }
}

public ref struct BlobReader
{
    private readonly ReadOnlySpan<byte> _source;

    public BlobReader(ReadOnlySpan<byte> source)
    {
        _source = source;
        Read = 0;
    }

    public int Read { get; private set; }

    public int ReadInt() => BinaryPrimitives.ReadInt32LittleEndian(Take(4));

    public float ReadFloat() => BinaryPrimitives.ReadSingleLittleEndian(Take(4));

    public Vector2 ReadVector() => new(ReadFloat(), ReadFloat());

    public string ReadText() => Encoding.UTF8.GetString(Take(ReadInt()));

    private ReadOnlySpan<byte> Take(int count)
    {
        ReadOnlySpan<byte> slice = _source.Slice(Read, count);

        Read += count;

        return slice;
    }
}

public static class Writer1
{
    public const bool NotDone = true;

    public static long Measure(Action action)
    {
        action();
        action();
        action();

        long before = GC.GetAllocatedBytesForCurrentThread();
        action();

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    public static int WriteSave(Span<byte> destination, string name, int level, Vector2 position)
    {
        var writer = new BlobWriter(destination);

        writer.WriteText(name);
        writer.WriteInt(level);
        writer.WriteVector(position);

        return writer.Written;
    }

    public static void Run()
    {
        Span<byte> buffer = stackalloc byte[64];

        int written = WriteSave(buffer, "anna", 7, new Vector2(12.5f, -40.25f));

        Check.Equal(written, 4 + 4 + 4 + 8,
            "vingt octets : quatre pour la longueur du nom, quatre pour le nom, quatre pour le niveau, huit pour la position. Un format binaire n'a AUCUN caractere de decoration");

        var reader = new BlobReader(buffer.Slice(0, written));

        Check.Equal(reader.ReadText(), "anna", "on relit dans le MEME ordre qu'on a ecrit, sans exception");
        Check.Equal(reader.ReadInt(), 7, "il n'y a pas de noms de champs dans le fichier : c'est le CODE qui est le format");
        Check.Near(reader.ReadVector(), new Vector2(12.5f, -40.25f), "et la position au bit pres, parce qu'on a ecrit les octets du float");
        Check.Equal(reader.Read, written, "tout a ete consomme : un lecteur qui finit ailleurs qu'a la fin est un lecteur desynchronise");

        Check.True(written < 60, "a comparer avec le JSON equivalent, qui en ferait plus du double");

        Check.Equal(Measure(() => { Span<byte> scratch = stackalloc byte[64]; WriteSave(scratch, "anna", 7, Vector2.Zero); }), 0L,
            "et l'ecriture entiere n'alloue RIEN : un 'ref struct' ne peut vivre que sur la pile, et le compilateur l'impose");

        Span<byte> tiny = stackalloc byte[8];

        Check.Throws<InvalidOperationException>(() => { Span<byte> small = stackalloc byte[8]; WriteSave(small, "anna", 7, Vector2.Zero); },
            "un tampon trop petit doit lever. Sans ce controle, on ecrit dans la pile de l'appelant et le plantage sort ailleurs, plus tard, sans rapport");

        Check.Throws<InvalidOperationException>(() =>
            {
                var truncated = new BlobReader(new byte[] { 10, 0, 0, 0, (byte)'a', (byte)'n' });

                truncated.ReadText();
            },
            "et un fichier TRONQUE - coupure de courant pendant la sauvegarde - doit lever aussi, au lieu de rendre une chaine de longueur inventee");

        Span<byte> unicode = stackalloc byte[64];
        int size = WriteSave(unicode, "epee tres longue", 1, Vector2.Zero);
        var back = new BlobReader(unicode.Slice(0, size));

        Check.Equal(back.ReadText(), "epee tres longue",
            "une chaine s'ecrit PREFIXEE de sa longueur en octets, pas terminee par un zero : c'est plus court a lire, ca supporte les octets nuls, et ca permet de sauter le champ sans le decoder");

        Check.Equal(MemoryMarshal.Read<int>(buffer), 4, "et les quatre premiers octets du fichier sont bien la longueur du nom");
    }
}
