using System.Numerics;

namespace Csharplings;

public static class Pack1
{
    public const bool NotDone = false;

    public const int HealthBits = 10;
    public const int TeamBits = 3;
    public const int StateBits = 4;

    public static uint Pack(int health, int team, int state, bool visible)
    {
        return (uint)(health & 0x3FF)
            | ((uint)(team & 0x7) << HealthBits)
            | ((uint)(state & 0xF) << (HealthBits + TeamBits))
            | (visible ? 1u << (HealthBits + TeamBits + StateBits) : 0u);
    }

    public static int Health(uint packed) => (int)(packed & 0x3FF);

    public static int Team(uint packed) => (int)((packed >> HealthBits) & 0x7);

    public static int State(uint packed) => (int)((packed >> (HealthBits + TeamBits)) & 0xF);

    public static bool Visible(uint packed) => ((packed >> (HealthBits + TeamBits + StateBits)) & 1u) != 0u;

    public static uint WithHealth(uint packed, int health) => (packed & ~0x3FFu) | (uint)(health & 0x3FF);

    public static void Run()
    {
        uint packed = Pack(health: 750, team: 3, state: 9, visible: true);

        Check.Equal(Health(packed), 750, "dix bits suffisent pour zero a mille vingt-trois points de vie");
        Check.Equal(Team(packed), 3, "trois bits pour huit equipes");
        Check.Equal(State(packed), 9, "quatre bits pour seize etats");
        Check.True(Visible(packed), "et un bit pour un booleen, au lieu d'un octet entier");

        Check.Equal(HealthBits + TeamBits + StateBits + 1, 18,
            "dix-huit bits en tout : quatre champs qui tiennent dans un seul entier de quatre octets, la ou quatre champs separes en prendraient au moins sept");

        uint hurt = WithHealth(packed, 12);

        Check.Equal(Health(hurt), 12, "modifier un champ, c'est effacer ses bits puis poser les nouveaux");
        Check.Equal(Team(hurt), 3, "les voisins ne bougent pas, a condition que le MASQUE soit juste");
        Check.Equal(State(hurt), 9, "aucun");
        Check.True(Visible(hurt), "aucun du tout");

        Check.Equal(Health(Pack(2000, 0, 0, false)), 976,
            "et voila le danger : 2000 ne tient pas dans dix bits, le masque garde les bits du bas et le reste disparait SANS erreur. Un depassement en binaire ne leve jamais, il ment");

        Check.Equal((1 << HealthBits) - 1, 1023, "d'ou la regle : la valeur maximale d'un champ de n bits est 2 puissance n moins un, et il faut la verifier soi-meme");

        Check.Equal(Pack(0, 0, 0, false), 0u, "tout a zero donne zero");
        Check.Equal(Pack(1023, 7, 15, true), 0x3FFFFu, "et tout au maximum remplit exactement les dix-huit bits");

        Check.Equal(BitOperations.PopCount(0b1011u), 3, "PopCount compte les bits a un : de quoi compter les entites d'un masque d'ECS en une instruction");
        Check.Equal(BitOperations.TrailingZeroCount(0b1000u), 3, "TrailingZeroCount trouve le premier bit a un : c'est l'iteration d'un masque de composants");
        Check.Equal(BitOperations.LeadingZeroCount(1u), 31, "et LeadingZeroCount donne le rang du bit le plus haut");
        Check.Equal(BitOperations.RoundUpToPowerOf2(100u), 128u, "arrondir a la puissance de deux superieure : la taille d'un tampon ou d'une table de hachage");
        Check.True(BitOperations.IsPow2(64), "et savoir si c'en est une");

        uint flags = 0;

        flags |= 1u << 5;

        Check.True((flags & (1u << 5)) != 0u, "poser un drapeau : ou binaire avec le bit");

        flags &= ~(1u << 5);

        Check.Equal(flags, 0u, "l'enlever : et binaire avec son complement");

        flags ^= 1u << 2;

        Check.True((flags & (1u << 2)) != 0u, "et le basculer : ou exclusif, la seule des trois qui n'a pas besoin de connaitre l'etat d'avant");
    }
}
