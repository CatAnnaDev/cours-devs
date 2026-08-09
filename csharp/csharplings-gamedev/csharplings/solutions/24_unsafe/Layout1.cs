using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Csharplings;

public struct Wasteful
{
    public byte Flag;

    public int Health;

    public byte Team;
}

public struct Packed
{
    public int Health;

    public byte Flag;

    public byte Team;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Tight
{
    public byte Flag;

    public int Health;

    public byte Team;
}

[StructLayout(LayoutKind.Explicit)]
public struct ColorBits
{
    [FieldOffset(0)]
    public uint Packed;

    [FieldOffset(0)]
    public byte Red;

    [FieldOffset(1)]
    public byte Green;

    [FieldOffset(2)]
    public byte Blue;

    [FieldOffset(3)]
    public byte Alpha;
}

public static class Layout1
{
    public const bool NotDone = false;

    public static int SizeOf<T>() => Unsafe.SizeOf<T>();

    public static void Run()
    {
        Check.Equal(SizeOf<Wasteful>(), 12,
            "un octet, un entier, un octet : douze octets au lieu de six. Le compilateur ALIGNE chaque champ sur sa propre taille et bouche les trous");

        Check.Equal(SizeOf<Packed>(), 8,
            "les MEMES champs, du plus grand au plus petit : huit octets. Ranger ses champs par taille decroissante est la seule optimisation de ce fichier qui ne coute rien");

        Check.Equal(SizeOf<Tight>(), 6,
            "et Pack = 1 supprime tout alignement : six octets, la somme exacte. A ne faire que pour un format de fichier ou un protocole, jamais pour de la donnee chaude");

        Check.True(SizeOf<Wasteful>() > SizeOf<Packed>(),
            "sur dix mille entites, ces quatre octets font quarante kilooctets, et surtout un tiers de lignes de cache en plus a lire a chaque parcours");

        Check.Equal(SizeOf<Vector2>(), 8, "un Vector2 est deja compact : deux floats et rien autour");

        Check.Equal(SizeOf<byte>(), 1, "un octet");
        Check.Equal(SizeOf<char>(), 2, "un char en pese deux : le C# est en UTF-16");
        Check.Equal(SizeOf<bool>(), 1, "un bool en pese un, meme s'il n'utilise qu'un bit");

        var color = new ColorBits { Red = 0x11, Green = 0x22, Blue = 0x33, Alpha = 0x44 };

        Check.Equal(SizeOf<ColorBits>(), 4, "une UNION : cinq champs qui se partagent les memes quatre octets");
        Check.Equal(color.Packed, 0x44332211u,
            "ecrire les quatre composantes et relire l'entier : c'est la MEME memoire, vue de deux facons. Les octets sortent a l'envers parce que la machine est petit-boutiste");

        color.Packed = 0xAABBCCDDu;

        Check.Equal(color.Red, (byte)0xDD, "et l'inverse marche : ecrire l'entier renseigne les quatre composantes");
        Check.Equal(color.Alpha, (byte)0xAA, "sans un seul decalage de bits ecrit a la main");

        Check.Equal(Marshal.SizeOf<Packed>(), 8, "Marshal.SizeOf donne la taille de la version MARSHALEE, celle que verrait du code natif");

        Check.True(SizeOf<Packed>() == Marshal.SizeOf<Packed>(),
            "les deux coincident ici parce que Packed est blittable : ses champs ont la meme forme des deux cotes de la frontiere, donc rien a convertir");

        Check.True(RuntimeHelpers.IsReferenceOrContainsReferences<string>(),
            "un type qui contient une reference ne peut pas etre recopie octet par octet");

        Check.False(RuntimeHelpers.IsReferenceOrContainsReferences<Packed>(),
            "un struct qui n'en contient aucune, si. C'est le test exact qui autorise MemoryMarshal, stackalloc, un envoi reseau ou une ecriture disque directe");

        Check.False(RuntimeHelpers.IsReferenceOrContainsReferences<Vector2>(),
            "et c'est pour ca qu'un tableau de Vector2 se passe tel quel a une API native, la ou un tableau de classes demanderait une conversion element par element");
    }
}
