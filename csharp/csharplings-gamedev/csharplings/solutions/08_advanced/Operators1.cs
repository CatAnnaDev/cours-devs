namespace Csharplings;

public readonly struct Damage : IEquatable<Damage>
{
    public Damage(int physical, int magic)
    {
        Physical = physical;
        Magic = magic;
    }

    public int Physical { get; }
    public int Magic { get; }

    public int Total => Physical + Magic;

    public static Damage operator +(Damage a, Damage b) => new(a.Physical + b.Physical, a.Magic + b.Magic);

    public static Damage operator *(Damage a, int factor) => new(a.Physical * factor, a.Magic * factor);

    public static bool operator >(Damage a, Damage b) => a.Total > b.Total;

    public static bool operator <(Damage a, Damage b) => a.Total < b.Total;

    public static bool operator ==(Damage a, Damage b) => a.Equals(b);

    public static bool operator !=(Damage a, Damage b) => !a.Equals(b);

    public static explicit operator int(Damage damage) => damage.Total;

    public bool Equals(Damage other) => Physical == other.Physical && Magic == other.Magic;

    public override bool Equals(object obj) => obj is Damage other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Physical, Magic);

    public override string ToString() => $"{Physical}p+{Magic}m";
}

public static class Operators1
{
    public const bool NotDone = false;

    public static void Run()
    {
        var sword = new Damage(12, 0);
        var staff = new Damage(2, 15);

        Check.Equal((sword + staff).Physical, 14, "l'addition marche composante par composante");
        Check.Equal((sword + staff).Magic, 15, "sans oublier la magie");
        Check.Equal((sword * 3).Total, 36, "un multiplicateur de degats critiques");

        Check.True(staff > sword, "la comparaison porte sur le total");
        Check.False(staff < sword, "et il faut declarer les deux sens, sinon ca ne compile pas");

        Check.True(sword == new Damage(12, 0), "deux degats identiques sont egaux");
        Check.True(sword != staff, "et deux differents ne le sont pas");
        Check.False(sword.Equals("epee"), "Equals(object) doit refuser tout ce qui n'est pas un Damage");

        Check.Equal((int)staff, 17, "une conversion explicite vers le total");
        Check.Equal(new Damage(1, 1).ToString(), "1p+1m", "un ToString lisible dans les logs");

        var seen = new HashSet<Damage> { sword, new Damage(12, 0), staff };

        Check.Equal(seen.Count, 2, "== sans GetHashCode assorti, c'est le bug classique");
    }
}
