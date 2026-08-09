namespace Csharplings;

public enum Element
{
    Fire,
    Water,
    Grass,
}

public static class Enums1
{
    public const bool NotDone = true;

    public static Element Beats(Element element)
    {
        return Todo.Value<Element>();
    }

    public static bool WinsAgainst(Element attacker, Element defender)
    {
        return Beats(attacker) == defender;
    }

    public static void Run()
    {
        Check.Equal(Beats(Element.Fire), Element.Grass, "le feu brule l'herbe");
        Check.Equal(Beats(Element.Grass), Element.Water, "l'herbe boit l'eau");
        Check.Equal(Beats(Element.Water), Element.Fire, "l'eau eteint le feu");

        Check.True(WinsAgainst(Element.Fire, Element.Grass), "feu contre herbe");
        Check.False(WinsAgainst(Element.Fire, Element.Water), "feu contre eau");

        Check.Equal((int)Element.Fire, 0, "un enum est un entier sous le capot, il commence a 0");
        Check.Equal(Element.Water.ToString(), "Water", "mais il s'affiche avec son nom");
    }
}
