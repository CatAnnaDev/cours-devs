using System.Numerics;

namespace Csharplings;

public enum ComponentKind
{
    Position,
    Velocity,
    Health,
    Sprite,
    Player,
}

public readonly struct ComponentMask : IEquatable<ComponentMask>
{
    private ComponentMask(ulong bits)
    {
        Bits = bits;
    }

    public ulong Bits { get; }

    public static ComponentMask Empty => new ComponentMask(0UL);

    public int Count => BitOperations.PopCount(Bits);

    public static ComponentMask Of(params ComponentKind[] kinds)
    {
        ulong bits = 0UL;

        foreach (ComponentKind kind in kinds)
            bits |= Bit(kind);

        return new ComponentMask(bits);
    }

    public ComponentMask With(ComponentKind kind) => new ComponentMask(Bits | Bit(kind));

    public ComponentMask Without(ComponentKind kind) => new ComponentMask(Bits & ~Bit(kind));

    public bool Has(ComponentKind kind) => (Bits & Bit(kind)) != 0UL;

    public bool HasAll(ComponentMask required) => (Bits & required.Bits) == required.Bits;

    public bool HasAny(ComponentMask any) => (Bits & any.Bits) != 0UL;

    private static ulong Bit(ComponentKind kind) => 1UL << (int)kind;

    public bool Equals(ComponentMask other) => Bits == other.Bits;

    public override bool Equals(object obj) => obj is ComponentMask other && Equals(other);

    public override int GetHashCode() => Bits.GetHashCode();

    public override string ToString() => $"mask({Bits})";
}

public static class Masks1
{
    public const bool NotDone = false;

    public static void Run()
    {
        ComponentMask movers = ComponentMask.Of(ComponentKind.Position, ComponentKind.Velocity);

        Check.Equal(movers.Bits, 3UL, "Position occupe le bit 1, Velocity le bit 2 : ensemble ca fait 3");
        Check.Equal(ComponentMask.Of(ComponentKind.Health).Bits, 4UL, "chaque composant vaut une puissance de deux");
        Check.Equal(ComponentMask.Of(ComponentKind.Player).Bits, 16UL, "le cinquieme vaut 16, jamais 5");
        Check.Equal(ComponentMask.Empty.Bits, 0UL, "un masque vide ne porte aucun bit");

        Check.True(movers.Has(ComponentKind.Position), "le bit de Position est la");
        Check.False(movers.Has(ComponentKind.Health), "celui de Health non");
        Check.Equal(movers.Count, 2, "deux composants dans le masque, quel que soit le nombre de types existants");

        ComponentMask fighter = movers.With(ComponentKind.Health);

        Check.Equal(fighter.Bits, 7UL, "ajouter un composant, c'est un OU binaire");
        Check.Equal(movers.Bits, 3UL, "et le masque d'origine n'a pas bouge : c'est une structure, elle a ete copiee");
        Check.Equal(movers.With(ComponentKind.Position).Bits, 3UL, "ajouter deux fois le meme ne change rien");

        Check.Equal(fighter.Without(ComponentKind.Velocity).Bits, 5UL, "retirer, c'est un ET avec le complement");
        Check.Equal(fighter.Without(ComponentKind.Sprite).Bits, 7UL, "retirer un composant absent ne change rien");

        Check.True(fighter.HasAll(movers),
            "une entite Position+Velocity+Health repond bien a une requete Position+Velocity");
        Check.False(movers.HasAll(fighter),
            "l'inverse est faux : il lui manque Health. HasAll compare au masque demande, PAS a zero");
        Check.False(movers.HasAll(ComponentMask.Of(ComponentKind.Position, ComponentKind.Health)),
            "un seul bit commun ne suffit pas pour HasAll");

        Check.True(movers.HasAny(ComponentMask.Of(ComponentKind.Health, ComponentKind.Position)),
            "HasAny, lui, se contente d'un seul bit en commun");
        Check.False(movers.HasAny(ComponentMask.Of(ComponentKind.Health, ComponentKind.Sprite)),
            "aucun bit commun, aucune correspondance");

        Check.True(movers.HasAll(ComponentMask.Empty), "tout le monde repond a une requete vide");
        Check.False(ComponentMask.Empty.HasAny(movers), "mais un masque vide n'a rien en commun avec personne");

        var world = new[]
        {
            ComponentMask.Of(ComponentKind.Position, ComponentKind.Velocity, ComponentKind.Sprite),
            ComponentMask.Of(ComponentKind.Position, ComponentKind.Sprite),
            ComponentMask.Of(ComponentKind.Position, ComponentKind.Velocity, ComponentKind.Health, ComponentKind.Player),
            ComponentMask.Empty,
            ComponentMask.Of(ComponentKind.Velocity),
        };

        var matching = new List<int>();

        for (int entity = 0; entity < world.Length; entity++)
        {
            if (world[entity].HasAll(movers))
                matching.Add(entity);
        }

        Check.Sequence(matching, new[] { 0, 2 },
            "une requete sur 5 entites : deux tests binaires par entite, et c'est tout le secret des ECS rapides");

        Check.Equal(ComponentMask.Of(ComponentKind.Position, ComponentKind.Velocity), movers,
            "deux masques faits des memes composants sont egaux, dans n'importe quel ordre");
        Check.Equal(ComponentMask.Of(ComponentKind.Velocity, ComponentKind.Position), movers,
            "l'ordre des composants n'existe pas dans un masque");
    }
}
