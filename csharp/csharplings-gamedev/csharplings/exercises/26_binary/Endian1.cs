using System.Buffers.Binary;

namespace Csharplings;

public static class Endian1
{
    public const bool NotDone = true;

    public static byte[] WriteLittle(int value)
    {
        var buffer = new byte[4];

        BinaryPrimitives.WriteInt32LittleEndian(buffer, value);

        return buffer;
    }

    public static byte[] WriteBig(int value)
    {
        var buffer = new byte[4];

        BinaryPrimitives.WriteInt32LittleEndian(buffer, value);

        return buffer;
    }

    public static int ReadLittle(ReadOnlySpan<byte> raw) => BinaryPrimitives.ReadInt32LittleEndian(raw);

    public static int ReadBig(ReadOnlySpan<byte> raw) => Todo.Value<int>();

    public static void Run()
    {
        Check.Sequence(WriteLittle(0x11223344), new byte[] { 0x44, 0x33, 0x22, 0x11 },
            "petit-boutiste : l'octet de POIDS FAIBLE en premier. C'est l'ordre de x86 et d'ARM, donc de toutes les machines de joueurs");

        Check.Sequence(WriteBig(0x11223344), new byte[] { 0x11, 0x22, 0x33, 0x44 },
            "gros-boutiste : l'octet de poids fort en premier. C'est l'ordre reseau, celui de tous les protocoles depuis TCP");

        Check.Equal(ReadLittle(WriteLittle(-1234)), -1234, "ecrire et relire dans le meme ordre rend la valeur");

        Check.Equal(ReadBig(WriteLittle(0x11223344)), 0x44332211,
            "les melanger rend un nombre PARFAITEMENT valide et completement faux. Aucune exception, aucun avertissement : c'est ce qui rend le bug si long a trouver");

        Check.True(BitConverter.IsLittleEndian,
            "et voila le piege : la machine de developpement est petit-boutiste, donc un code qui suppose l'ordre machine marche partout... jusqu'au premier appareil qui ne l'est pas");

        Check.Equal(BinaryPrimitives.ReverseEndianness(0x11223344), 0x44332211,
            "ReverseEndianness retourne un entier, quand il faut convertir une valeur deja lue");

        Check.Equal(BinaryPrimitives.ReverseEndianness((ushort)0x1122), (ushort)0x2211, "il existe pour chaque taille");

        var floats = new byte[4];

        BinaryPrimitives.WriteSingleLittleEndian(floats, 1.5f);

        Check.Equal(BinaryPrimitives.ReadSingleLittleEndian(floats), 1.5f, "les flottants aussi ont un ordre d'octets");

        Check.Sequence(floats, new byte[] { 0x00, 0x00, 0xC0, 0x3F },
            "et leur representation est celle de la norme IEEE 754, identique sur toutes les machines : seul l'ORDRE des octets change, jamais leur contenu");

        var wide = new byte[8];

        BinaryPrimitives.WriteInt64BigEndian(wide, 1L);

        Check.Equal(wide[7], (byte)1, "en gros-boutiste, le un est le dernier octet");
        Check.Equal(wide[0], (byte)0, "et les sept premiers sont nuls");

        Check.Throws<ArgumentOutOfRangeException>(() => BinaryPrimitives.ReadInt32LittleEndian(new byte[2]),
            "lire quatre octets dans un tampon qui n'en a que deux echoue franchement, au lieu de lire ce qui traine derriere");

        Check.Equal(ReadLittle(WriteBig(1)), 16777216,
            "la regle, donc : on ECRIT toujours l'ordre dans le format, une fois pour toutes. Un fichier de sauvegarde peut rester en petit-boutiste, un paquet reseau se fait en gros-boutiste, et un lecteur ne devine jamais");
    }
}
