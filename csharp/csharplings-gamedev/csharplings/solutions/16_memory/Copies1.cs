namespace Csharplings;

public struct MutableStats
{
    public int Attack;

    public int Bump()
    {
        Attack++;

        return Attack;
    }
}

public readonly struct FrozenStats
{
    public FrozenStats(int attack, int armor)
    {
        Attack = attack;
        Armor = armor;
    }

    public int Attack { get; }
    public int Armor { get; }

    public FrozenStats WithAttack(int attack) => new FrozenStats(attack, Armor);

    public FrozenStats Buffed(int bonus) => new FrozenStats(Attack + bonus, Armor);
}

public static class Copies1
{
    public const bool NotDone = false;

    private static MutableStats _normal = new MutableStats { Attack = 1 };

    private static readonly MutableStats Frozen = new MutableStats { Attack = 1 };

    public static int BumpThroughIn(in MutableStats stats) => stats.Bump();

    public static void Run()
    {
        _normal.Bump();

        Check.Equal(_normal.Attack, 2, "sur un champ normal, la methode modifie bien la structure");

        Frozen.Bump();

        Check.Equal(Frozen.Attack, 1,
            "sur un champ 'readonly', le compilateur fabrique une COPIE avant d'appeler la methode : la modification part a la poubelle");

        var local = new MutableStats { Attack = 1 };
        BumpThroughIn(in local);

        Check.Equal(local.Attack, 1,
            "'in' promet de ne rien modifier : meme copie defensive, meme modification perdue");

        Check.Equal(BumpThroughIn(in local), 2, "la methode a bien renvoye 2, mais sur une copie que personne ne garde");

        var direct = new MutableStats { Attack = 1 };
        direct.Bump();

        Check.Equal(direct.Attack, 2, "sur une variable locale ordinaire, aucune copie : ca marche comme prevu");

        var frozen = new FrozenStats(10, 5);
        FrozenStats stronger = frozen.Buffed(5);

        Check.Equal(frozen.Attack, 10, "une structure 'readonly' ne peut pas etre modifiee du tout");
        Check.Equal(stronger.Attack, 15, "on en fabrique une nouvelle a la place");
        Check.Equal(stronger.Armor, 5, "en recopiant le reste");

        Check.Equal(frozen.WithAttack(99).Attack, 99, "une methode 'With' se chaine sans surprise");
        Check.Equal(frozen.Attack, 10, "et l'originale ne bouge jamais");

        var list = new List<MutableStats> { new MutableStats { Attack = 1 } };
        list[0].Bump();

        Check.Equal(list[0].Attack, 1,
            "list[0] rend une copie : appeler une methode dessus modifie un objet temporaire, pas la liste");

        var array = new MutableStats[1];
        array[0].Attack = 1;
        array[0].Bump();

        Check.Equal(array[0].Attack, 2,
            "un element de TABLEAU est une vraie variable : la methode travaille sur l'original");
    }
}
