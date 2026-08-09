namespace Csharplings;

public static class Quantize1
{
    public const bool NotDone = true;

    public const float WorldMin = -1000f;
    public const float WorldMax = 1000f;

    public static ushort QuantizePosition(float value)
    {
        float normalized = (value - WorldMin) / (WorldMax - WorldMin);

        return (ushort)Mathf.RoundToInt(normalized * ushort.MaxValue);
    }

    public static float RestorePosition(ushort packed) =>
        WorldMin + (packed / (float)ushort.MaxValue) * (WorldMax - WorldMin);

    public static byte QuantizeAngle(float radians)
    {
        float turns = Mathf.Clamp(radians / (Mathf.Pi * 2f), 0f, 1f);

        return (byte)(Mathf.RoundToInt(turns * 256f) & 0xFF);
    }

    public static float RestoreAngle(byte packed) => packed / 256f * (Mathf.Pi * 2f);

    public static sbyte QuantizeUnit(float value) =>
        (sbyte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp(value, -1f, 1f) * 127f), -127, 127);

    public static float RestoreUnit(sbyte packed) => packed / 127f;

    public static float WorstPositionError() => (WorldMax - WorldMin) / ushort.MaxValue / 2f;

    public static void Run()
    {
        Check.Equal(QuantizePosition(WorldMin), (ushort)0, "le bas de la plage tombe sur zero");
        Check.Equal(QuantizePosition(WorldMax), ushort.MaxValue, "le haut sur le maximum : toute la precision sert a la plage utile");

        Check.Near(RestorePosition(QuantizePosition(123.456f)), 123.456f, "un aller-retour rend presque la valeur d'origine", WorstPositionError());

        Check.True(WorstPositionError() < 0.02f,
            $"presque, et on sait de combien : {WorstPositionError()} unite au pire sur deux mille. Deux octets au lieu de quatre, pour une erreur plus petite qu'un pixel");

        Check.Near(RestorePosition(QuantizePosition(3000f)), WorldMax, "hors de la plage, la valeur est PLAQUEE au bord", WorstPositionError());

        Check.True(QuantizePosition(-5000f) == 0,
            "dans les deux sens. Sans ce plaquage, la conversion deborde et un joueur tombe a l'autre bout de la carte : c'est le bug de reseau le plus spectaculaire qui soit");

        Check.Equal(QuantizeAngle(0f), (byte)0, "un angle se quantifie sur UN octet");
        Check.Equal(QuantizeAngle(Mathf.Pi), (byte)128, "un demi-tour tombe pile au milieu");

        Check.Near(RestoreAngle(QuantizeAngle(1.2f)), 1.2f, "et l'aller-retour tient a un cinquantieme de radian", 0.02f);

        Check.Equal(QuantizeAngle(Mathf.Pi * 2f), (byte)0,
            "un tour complet revient a zero : un angle est CYCLIQUE, donc il ne se plaque pas, il s'enroule. Utiliser un plaquage ici collerait toutes les rotations au meme bord");

        Check.Equal(QuantizeAngle(-Mathf.Pi), (byte)128, "et les angles negatifs s'enroulent aussi");

        Check.Equal(QuantizeUnit(0f), (sbyte)0, "une composante de vecteur normalise tient sur un octet signe");
        Check.Equal(QuantizeUnit(1f), (sbyte)127, "un pour le maximum");
        Check.Equal(QuantizeUnit(-1f), (sbyte)(-127), "et son oppose exact : on n'utilise PAS -128, pour que zero reste zero et que la plage soit symetrique");

        Check.Near(RestoreUnit(QuantizeUnit(0.5f)), 0.5f, "l'erreur reste sous un centieme", 0.01f);

        Check.Equal(sizeof(float) * 3, 12, "une normale en trois floats pese douze octets");
        Check.Equal(sizeof(sbyte) * 3, 3, "quantifiee, elle en pese trois : quatre fois moins, pour une precision que l'oeil ne distingue pas");

        Check.Near(RestorePosition(QuantizePosition(0f)), 0f, "dernier detail qui compte : zero doit retomber sur zero", WorstPositionError());
    }
}
