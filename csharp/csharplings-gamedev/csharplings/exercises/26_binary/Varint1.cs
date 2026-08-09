namespace Csharplings;

public static class Varint1
{
    public const bool NotDone = true;

    public static int WriteVarint(Span<byte> destination, uint value)
    {
        int written = 0;

        while (value >= 0x80)
        {
            destination[written++] = (byte)(value | 0x80);
            value >>= 7;
        }

        destination[written++] = (byte)value;

        return written;
    }

    public static uint ReadVarint(ReadOnlySpan<byte> source, out int read)
    {
        uint value = 0;
        int shift = 0;

        read = 0;

        while (true)
        {
            byte current = source[read++];

            value |= (uint)(current & 0x7F) << shift;

            if ((current & 0x80) == 0)
                return value;

            shift += 7;
        }
    }

    public static uint ZigZag(int value) => (uint)(value < 0 ? -value * 2 : value * 2);

    public static int UnZigZag(uint value) => (int)(value >> 1) ^ -(int)(value & 1);

    public static int SizeOf(uint value)
    {
        Span<byte> scratch = stackalloc byte[5];

        return WriteVarint(scratch, value);
    }

    public static void Run()
    {
        Span<byte> buffer = stackalloc byte[8];

        Check.Equal(WriteVarint(buffer, 0), 1, "zero tient sur un octet");
        Check.Equal(WriteVarint(buffer, 127), 1, "et tout ce qui tient sur sept bits aussi");
        Check.Equal(WriteVarint(buffer, 128), 2, "au-dela, il en faut deux : le huitieme bit dit 'il y a une suite'");
        Check.Equal(WriteVarint(buffer, 300), 2, "trois cents en deux octets");
        Check.Equal(WriteVarint(buffer, uint.MaxValue), 5,
            "et le pire cas en cinq : un varint peut etre plus GROS qu'un entier fixe. Il gagne parce que dans un jeu, presque tous les nombres sont petits");

        WriteVarint(buffer, 300);

        Check.Equal(ReadVarint(buffer, out int read), 300u, "la relecture rend la valeur");
        Check.Equal(read, 2, "et dit combien d'octets elle a consommes, sans quoi on ne saurait pas ou commence le champ suivant");

        for (uint value = 0; value < 5000; value += 137)
        {
            WriteVarint(buffer, value);

            Check.Equal(ReadVarint(buffer, out _), value, value == 0 ? "l'aller-retour tient sur toute la plage" : "toujours");
        }

        Check.Equal(SizeOf(1), 1, "un identifiant d'objet en dessous de 128 : un octet au lieu de quatre");
        Check.Equal(SizeOf(1000), 2, "mille : deux octets");
        Check.Equal(SizeOf(100_000), 3, "cent mille : trois");

        Check.Equal(ZigZag(0), 0u, "les negatifs sont le probleme : -1 en complement a deux, c'est 0xFFFFFFFF, donc cinq octets de varint");
        Check.Equal(ZigZag(-1), 1u, "le zigzag entrelace : 0, -1, 1, -2 deviennent 0, 1, 2, 3");
        Check.Equal(ZigZag(1), 2u, "les petits nombres restent petits, quel que soit leur signe");
        Check.Equal(ZigZag(-2), 3u, "et l'encodage n'est plus catastrophique du cote negatif");

        Check.Equal(UnZigZag(ZigZag(-12345)), -12345, "l'operation est reversible");
        Check.Equal(UnZigZag(ZigZag(int.MinValue)), int.MinValue, "sur toute la plage, bornes comprises");

        Check.Equal(SizeOf(ZigZag(-1)), 1, "un delta de -1 tient donc sur UN octet");
        Check.Equal(SizeOf(unchecked((uint)-1)), 5, "sans zigzag, il en aurait pris cinq");

        Check.Throws<FormatException>(() => ReadVarint(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, out _),
            "et un lecteur de varint DOIT se borner : sans le test de longueur, un fichier corrompu le fait boucler, et un paquet reseau malveillant aussi");
    }
}
