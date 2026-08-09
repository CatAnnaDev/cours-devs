namespace Csharplings;

public enum Element
{
    Physical,
    Fire,
    Ice,
}

public sealed class Fighter
{
    private readonly Dictionary<Element, float> _resistances = new();

    public Fighter(int maxHealth, int armor)
    {
        MaxHealth = maxHealth;
        Health = maxHealth;
        Armor = armor;
    }

    public int MaxHealth { get; }
    public int Health { get; private set; }
    public int Armor { get; }

    public bool IsAlive => Health > 0;

    public void Resist(Element element, float ratio) => _resistances[element] = ratio;

    public float ResistanceTo(Element element) => _resistances[element];

    public void SetHealth(int value) => Health = value;
}

public static class Damage1
{
    public const bool NotDone = true;

    public static int Compute(int raw, Element element, Fighter target, bool critical)
    {
        float value = raw;

        if (element == Element.Physical)
            value -= target.Armor;
        else
            value *= 1f - target.ResistanceTo(element);

        if (critical)
            value *= 2f;

        return Mathf.RoundToInt(value);
    }

    public static int Apply(int raw, Element element, Fighter target, bool critical)
    {
        int dealt = Compute(raw, element, target, critical);
        target.SetHealth(target.Health - dealt);

        return dealt;
    }

    public static void Run()
    {
        var knight = new Fighter(100, 8);
        knight.Resist(Element.Fire, 0.5f);
        knight.Resist(Element.Ice, -0.5f);

        Check.Equal(Compute(20, Element.Physical, knight, false), 12, "l'armure se soustrait aux degats physiques");
        Check.Equal(Compute(20, Element.Fire, knight, false), 10, "une resistance de 50 pour cent divise par deux");
        Check.Equal(Compute(20, Element.Ice, knight, false), 30, "une resistance negative, c'est une faiblesse");

        Check.Equal(Compute(20, Element.Physical, knight, true), 32, "le critique double AVANT l'armure, pas apres");
        Check.Equal(Compute(1, Element.Physical, knight, false), 1, "on ne soigne jamais l'ennemi : minimum un point");

        var rat = new Fighter(10, 0);

        Check.Equal(Apply(4, Element.Physical, rat, false), 4, "Apply rend les degats reellement infliges");
        Check.Equal(rat.Health, 6, "et les retire");

        Check.Equal(Apply(999, Element.Physical, rat, false), 6, "un coup mortel ne compte que ce qu'il restait");
        Check.Equal(rat.Health, 0, "les points de vie s'arretent a zero");
        Check.False(rat.IsAlive, "le rat est mort");

        Check.Equal(Apply(50, Element.Fire, rat, false), 0, "frapper un mort ne fait rien");
        Check.Equal(rat.Health, 0, "et ne descend pas dans le negatif");

        var immune = new Fighter(100, 0);
        immune.Resist(Element.Fire, 1f);

        Check.Equal(Compute(60, Element.Fire, immune, false), 1, "meme une immunite totale laisse passer le minimum");
    }
}
