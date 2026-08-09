using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Csharplings;

[StructLayout(LayoutKind.Sequential)]
public struct NativeTransform
{
    public float X;

    public float Y;

    public float Rotation;
}

[StructLayout(LayoutKind.Sequential)]
public struct WithText
{
    public int Id;

    public string Name;
}

public static unsafe class Interop1
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

    public static int Utf8Length(string text) => Encoding.UTF8.GetByteCount(text);

    public static int WriteUtf8(string text, Span<byte> destination)
    {
        int written = Encoding.UTF8.GetBytes(text, destination);

        destination[written] = 0;

        return written;
    }

    public static string ReadUtf8(ReadOnlySpan<byte> source)
    {
        int end = source.IndexOf((byte)0);

        return Encoding.UTF8.GetString(end < 0 ? source : source.Slice(0, end));
    }

    public static long AddressOf(int[] values)
    {
        GCHandle handle = GCHandle.Alloc(values, GCHandleType.Pinned);

        try
        {
            return handle.AddrOfPinnedObject().ToInt64();
        }
        finally
        {
            handle.Free();
        }
    }

    public static void Run()
    {
        Check.False(RuntimeHelpers.IsReferenceOrContainsReferences<NativeTransform>(),
            "trois floats et rien d'autre : le type est BLITTABLE, ses octets ont la meme forme des deux cotes de la frontiere");

        Check.Equal(Unsafe.SizeOf<NativeTransform>(), Marshal.SizeOf<NativeTransform>(),
            "la taille cote C# et la taille cote natif coincident, donc il n'y a rien a convertir : le passage est gratuit");

        Check.True(RuntimeHelpers.IsReferenceOrContainsReferences<WithText>(),
            "ajoute une string et ce n'est plus vrai : une reference C# n'a aucun sens pour du code natif");

        Check.Throws<ArgumentException>(() => GCHandle.Alloc(new WithText[2], GCHandleType.Pinned),
            "et on ne peut meme pas l'EPINGLER : le ramasse-miettes refuse de figer un objet qui contient des references, parce que le code natif ne saurait rien en faire");

        var transforms = new NativeTransform[2];

        transforms[1] = new NativeTransform { X = 3f, Y = 4f, Rotation = 1f };

        fixed (NativeTransform* pinned = transforms)
        {
            Check.Equal(pinned[1].X, 3f, "un tableau de types blittables s'epingle et se passe tel quel : zero copie, zero conversion");

            pinned[0].Y = 9f;
        }

        Check.Equal(transforms[0].Y, 9f, "et le natif ecrit directement dans TON tableau");

        long first = AddressOf(new int[4]);

        Check.True(first != 0L,
            "GCHandle epingle sans bloc 'fixed' : c'est ce qu'il faut quand l'adresse doit survivre a l'appel, par exemple un tampon que le moteur garde entre deux images");

        Check.Equal(Utf8Length("gobelin"), 7, "sept caracteres latins font sept octets en UTF-8");
        Check.Equal(Utf8Length("epee"), 4, "le C# stocke en UTF-16, le natif attend presque toujours de l'UTF-8 : il y a donc une CONVERSION, et elle alloue");

        Span<byte> buffer = stackalloc byte[32];

        buffer.Fill(0xFF);

        int written = WriteUtf8("gobelin", buffer);

        Check.Equal(written, 7, "convertir soi-meme dans un tampon qu'on fournit");
        Check.Equal(buffer[7], (byte)0,
            "sans oublier le ZERO final. Le tampon a ete rempli de 0xFF expres : dans la vraie vie il contient ce que l'appel precedent y a laisse, et une chaine native s'arrete au premier octet nul. L'oublier fait lire le code natif jusqu'au prochain, quelque part dans ta memoire");

        Check.Equal(ReadUtf8(buffer), "gobelin", "et le chemin retour s'arrete a ce meme zero");

        Check.Equal(Measure(() => { Span<byte> scratch = stackalloc byte[32]; scratch.Fill(0xFF); WriteUtf8("gobelin", scratch); }), 0L,
            "fait comme ca, le passage d'une chaine ne coute rien du tout. Marshal.StringToHGlobalAnsi, lui, alloue ET demande une liberation a la main");

        Check.Equal(Marshal.SizeOf<NativeTransform>(), 12, "douze octets, trois floats, l'ordre des champs garanti par LayoutKind.Sequential");

        Check.True(true,
            "la regle qui resume la frontiere : ce qui est blittable traverse gratuitement, le reste est converti a chaque appel. C'est aussi ce que dit 18_bridge sur Godot et 19_unity sur IL2CPP, vu depuis l'autre cote");
    }
}
