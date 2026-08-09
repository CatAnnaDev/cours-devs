namespace Demos.Pure;

public readonly struct Fixed : IEquatable<Fixed>, IComparable<Fixed>
{
    public const int FractionBits = 16;

    private const int OneRaw = 1 << FractionBits;

    private readonly int _raw;

    private Fixed(int raw)
    {
        _raw = raw;
    }

    public static Fixed Zero => new Fixed(0);

    public static Fixed One => new Fixed(OneRaw);

    public static Fixed Half => new Fixed(OneRaw >> 1);

    public static Fixed Epsilon => new Fixed(1);

    public int Raw => _raw;

    public static Fixed FromRaw(int raw) => new Fixed(raw);

    public static Fixed FromInt(int value) => new Fixed(value << FractionBits);

    public static Fixed FromParts(int whole, int numerator, int denominator) =>
        FromInt(whole) + FromInt(numerator) / FromInt(denominator);

    public int ToInt() => _raw >> FractionBits;

    public float ToFloat() => _raw / (float)OneRaw;

    public static Fixed operator +(Fixed a, Fixed b) => new Fixed(a._raw + b._raw);

    public static Fixed operator -(Fixed a, Fixed b) => new Fixed(a._raw - b._raw);

    public static Fixed operator -(Fixed a) => new Fixed(-a._raw);

    public static Fixed operator *(Fixed a, Fixed b) => new Fixed((int)(((long)a._raw * b._raw) >> FractionBits));

    public static Fixed operator /(Fixed a, Fixed b) => new Fixed((int)(((long)a._raw << FractionBits) / b._raw));

    public static bool operator <(Fixed a, Fixed b) => a._raw < b._raw;

    public static bool operator >(Fixed a, Fixed b) => a._raw > b._raw;

    public static bool operator <=(Fixed a, Fixed b) => a._raw <= b._raw;

    public static bool operator >=(Fixed a, Fixed b) => a._raw >= b._raw;

    public static bool operator ==(Fixed a, Fixed b) => a._raw == b._raw;

    public static bool operator !=(Fixed a, Fixed b) => a._raw != b._raw;

    public static Fixed Abs(Fixed value) => value._raw < 0 ? new Fixed(-value._raw) : value;

    public static Fixed Min(Fixed a, Fixed b) => a._raw < b._raw ? a : b;

    public static Fixed Max(Fixed a, Fixed b) => a._raw > b._raw ? a : b;

    public static Fixed Clamp(Fixed value, Fixed min, Fixed max) => Min(Max(value, min), max);

    public static Fixed Lerp(Fixed from, Fixed to, Fixed weight) => from + (to - from) * weight;

    public static Fixed Sqrt(Fixed value)
    {
        if (value._raw <= 0)
            return Zero;

        ulong remaining = (ulong)value._raw << FractionBits;
        ulong result = 0UL;
        ulong bit = 1UL << 46;

        while (bit > remaining)
            bit >>= 2;

        while (bit != 0UL)
        {
            if (remaining >= result + bit)
            {
                remaining -= result + bit;
                result = (result >> 1) + bit;
            }
            else
            {
                result >>= 1;
            }

            bit >>= 2;
        }

        return new Fixed((int)result);
    }

    public bool Equals(Fixed other) => _raw == other._raw;

    public override bool Equals(object obj) => obj is Fixed other && Equals(other);

    public override int GetHashCode() => _raw;

    public int CompareTo(Fixed other) => _raw.CompareTo(other._raw);

    public override string ToString() => ToFloat().ToString("0.#####");
}

public static class FixedPointDemo
{
    public static void Demo()
    {
        Console.WriteLine("--- Fixed : virgule fixe Q16.16, pour une simulation identique partout ---");

        Fixed tenth = Fixed.FromParts(0, 1, 10);
        Fixed sum = Fixed.Zero;

        for (int i = 0; i < 10; i++)
            sum += tenth;

        float floatSum = 0f;

        for (int i = 0; i < 10; i++)
            floatSum += 0.1f;

        Console.WriteLine($"  0.1 en Q16.16 vaut {tenth.Raw} / 65536 : il n'est PAS representable non plus");
        Console.WriteLine($"  dix fois 0.1 en Q16.16 : {sum.Raw} / 65536 = {sum}, donc pas 1.0 pile");
        Console.WriteLine($"  dix fois 0.1 en float  : {(double)floatSum:0.0000000000}, pas 1.0 pile non plus");
        Console.WriteLine($"  egal a 1 ?  virgule fixe {sum == Fixed.One}, float {floatSum == 1f}");
        Console.WriteLine("  ce n'est donc PAS l'exactitude qu'on achete. C'est que 65530 sera 65530 sur");
        Console.WriteLine("  toutes les machines, tous les compilateurs et toutes les architectures.");

        Fixed sixteenth = Fixed.FromParts(0, 1, 16);
        Fixed binarySum = Fixed.Zero;

        for (int i = 0; i < 16; i++)
            binarySum += sixteenth;

        Console.WriteLine($"  seize fois 1/16 : {binarySum} == 1 ? {binarySum == Fixed.One}   (une fraction BINAIRE, elle, est exacte)");

        Fixed gravity = Fixed.FromInt(-10);
        Fixed step = Fixed.FromParts(0, 1, 60);
        Fixed velocity = Fixed.Zero;
        Fixed height = Fixed.FromInt(100);

        for (int tick = 0; tick < 60; tick++)
        {
            velocity += gravity * step;
            height += velocity * step;
        }

        Console.WriteLine($"  une seconde de chute : hauteur {height}, vitesse {velocity}");
        Console.WriteLine("  rejoue mille fois sur mille machines, ce sera le MEME entier au bit pres");

        Console.WriteLine($"  racine de 2   : {Fixed.Sqrt(Fixed.FromInt(2))}   (attendu 1.41421)");
        Console.WriteLine($"  racine de 100 : {Fixed.Sqrt(Fixed.FromInt(100))}");
        Console.WriteLine($"  interpolation : {Fixed.Lerp(Fixed.Zero, Fixed.FromInt(10), Fixed.Half)}");

        Console.WriteLine("  le prix a payer : une plage limitee (environ -32768 a +32767) et une precision");
        Console.WriteLine("  figee (1/65536). En echange, aucune surprise de flottant, jamais.");
        Console.WriteLine();
    }
}
