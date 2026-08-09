namespace Csharplings;

public sealed class Player
{
    public string Name;
    public int Health;

    public string Describe()
    {
        return $"{Name} ({Health} PV)";
    }
}

public static class Classes1
{
    public const bool NotDone = true;

    public static void Run()
    {
        var anna = new Player("Anna", 100);
        var bob = new Player("Bob", 50);

        Check.Equal(anna.Describe(), "Anna (100 PV)", "le premier joueur");
        Check.Equal(bob.Describe(), "Bob (50 PV)", "le second joueur");

        anna.Health = 20;
        Check.Equal(anna.Describe(), "Anna (20 PV)", "chaque objet a ses propres donnees");
        Check.Equal(bob.Describe(), "Bob (50 PV)", "modifier l'un ne touche pas l'autre");
    }
}
