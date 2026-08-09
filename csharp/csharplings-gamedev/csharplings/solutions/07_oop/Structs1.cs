namespace Csharplings;

public struct Stats
{
    public int Health;
    public int Mana;
}

public sealed class Loadout
{
    public string Weapon;
}

public static class Structs1
{
    public const bool NotDone = false;

    public static void Heal(ref Stats stats, int amount)
    {
        stats.Health += amount;
    }

    public static void Equip(Loadout loadout, string weapon)
    {
        loadout.Weapon = weapon;
    }

    public static void Run()
    {
        var stats = new Stats { Health = 50, Mana = 10 };
        Stats copy = stats;
        copy.Health = 999;

        Check.Equal(stats.Health, 50, "un struct assigne est COPIE : l'original ne bouge pas");

        Heal(ref stats, 25);
        Check.Equal(stats.Health, 75, "pour modifier le vrai struct, il faut ref");

        var loadout = new Loadout { Weapon = "poing" };
        Loadout sameObject = loadout;
        sameObject.Weapon = "epee";

        Check.Equal(loadout.Weapon, "epee", "une class assignee est PARTAGEE : les deux pointent le meme objet");

        Equip(loadout, "arc");
        Check.Equal(loadout.Weapon, "arc", "une class passee a une methode est modifiable sans ref");
    }
}
