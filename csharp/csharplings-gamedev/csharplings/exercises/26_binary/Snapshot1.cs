namespace Csharplings;

public struct EntitySnapshot
{
    public int Health;

    public float X;

    public float Y;

    public int State;
}

public static class Snapshot1
{
    public const bool NotDone = true;

    public const uint HealthBit = 1u << 0;
    public const uint XBit = 1u << 1;
    public const uint YBit = 1u << 2;
    public const uint StateBit = 1u << 3;

    public static uint ChangedFields(in EntitySnapshot baseline, in EntitySnapshot current)
    {
        uint mask = 0;

        if (baseline.Health != current.Health)
            mask |= HealthBit;

        if (baseline.X != current.X)
            mask |= XBit;

        if (baseline.Y != current.Y)
            mask |= YBit;

        if (baseline.State != current.State)
            mask |= StateBit;

        return mask;
    }

    public static int PayloadSize(uint mask)
    {
        int size = 0;

        if ((mask & HealthBit) != 0)
            size += 4;

        if ((mask & XBit) != 0)
            size += 4;

        if ((mask & YBit) != 0)
            size += 4;

        if ((mask & StateBit) != 0)
            size += 4;

        return size;
    }

    public static EntitySnapshot Apply(in EntitySnapshot baseline, uint mask, in EntitySnapshot incoming)
    {
        EntitySnapshot result = incoming;

        if ((mask & HealthBit) != 0)
            result.Health = incoming.Health;

        if ((mask & XBit) != 0)
            result.X = incoming.X;

        if ((mask & YBit) != 0)
            result.Y = incoming.Y;

        if ((mask & StateBit) != 0)
            result.State = incoming.State;

        return result;
    }

    public static void Run()
    {
        var baseline = new EntitySnapshot { Health = 100, X = 10f, Y = 20f, State = 1 };
        var moved = baseline;

        moved.X = 11f;

        uint mask = ChangedFields(baseline, moved);

        Check.Equal(mask, XBit, "un seul champ a bouge, un seul bit est pose");
        Check.Equal(PayloadSize(mask), 5, "cinq octets sur le fil : un pour le masque, quatre pour le champ");
        Check.Equal(PayloadSize(0xF), 17, "contre dix-sept si on envoyait tout");

        EntitySnapshot rebuilt = Apply(baseline, mask, moved);

        Check.Equal(rebuilt.X, 11f, "le receveur applique le champ recu");
        Check.Equal(rebuilt.Y, 20f, "et garde les autres de sa BASELINE : c'est elle qui porte l'etat, le paquet ne porte que la difference");
        Check.Equal(rebuilt.Health, 100, "tous les autres");

        Check.Equal(ChangedFields(baseline, baseline), 0u, "rien n'a change, rien a envoyer");
        Check.Equal(PayloadSize(0), 1,
            "et meme dans ce cas on envoie l'octet de masque : il vaut zero, il dit 'cette entite est toujours la et elle n'a pas bouge', ce qui n'est pas la meme chose que le silence");

        var hurt = moved;

        hurt.Health = 40;
        hurt.State = 3;

        uint big = ChangedFields(moved, hurt);

        Check.Equal(big, HealthBit | StateBit, "deux champs, deux bits");
        Check.Equal(PayloadSize(big), 9, "neuf octets");

        EntitySnapshot afterTwo = Apply(moved, big, hurt);

        Check.Equal(afterTwo.Health, 40, "le receveur suit");
        Check.Equal(afterTwo.X, 11f, "et l'etat accumule reste juste tant que la CHAINE de baselines n'est pas rompue");

        EntitySnapshot desynchronized = Apply(baseline, big, hurt);

        Check.Equal(desynchronized.X, 10f,
            "voila le prix du delta : applique sur la MAUVAISE baseline, le resultat est faux et personne ne s'en apercoit. Un paquet perdu et l'entite reste a l'ancienne position pour toujours");

        Check.True(desynchronized.X != afterTwo.X,
            "d'ou les deux regles de tout protocole a delta : le receveur ACQUITTE la baseline qu'il possede, et l'emetteur renvoie un instantane COMPLET de temps en temps, pour que toute desynchronisation finisse par se corriger");

        int fullCost = 4 * PayloadSize(0xF);
        int deltaCost = PayloadSize(XBit) * 3 + PayloadSize(0xF);

        Check.True(deltaCost * 2 < fullCost,
            $"sur quatre images ou seule la position bouge : {deltaCost} octets contre {fullCost}. A soixante images par seconde et deux cents entites, c'est ce qui fait tenir la partie dans la bande passante d'un joueur");
    }
}
