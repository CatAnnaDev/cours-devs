namespace Csharplings;

public abstract record Entity;

public sealed record Hero(int Health, string Weapon) : Entity;

public sealed record Chest(int Gold, bool Locked) : Entity;

public sealed record Spike(int Damage) : Entity;

public static class Patterns1
{
    public const bool NotDone = false;

    public static string Describe(Entity entity) => entity switch
    {
        Hero { Health: 0 } => "mort",
        Hero { Health: < 30, Weapon: "epee" } => "en danger avec une epee",
        Hero hero => $"joueur a {hero.Health} pv",
        Chest { Locked: true } => "coffre verrouille",
        Chest chest => $"coffre de {chest.Gold} pieces",
        Spike spike when spike.Damage > 50 => "piege mortel",
        Spike => "piege",
        null => "rien",
        _ => "inconnu",
    };

    public static string Rank(int score)
    {
        return score switch
        {
            < 0 => "triche",
            0 => "zero",
            < 100 => "debutant",
            < 1000 => "confirme",
            _ => "legende",
        };
    }

    public static string Combo(int[] inputs)
    {
        return inputs switch
        {
            [] => "rien",
            [1] => "coup simple",
            [1, 1, 2] => "combo final",
            [1, .., 3] => "termine par un coup lourd",
            _ => "suite libre",
        };
    }

    public static bool TryLoot(Entity entity, out int gold)
    {
        if (entity is Chest { Locked: false } chest)
        {
            gold = chest.Gold;
            return true;
        }

        gold = 0;
        return false;
    }

    public static void Run()
    {
        Check.Equal(Describe(new Hero(0, "epee")), "mort", "le cas le plus precis passe en premier");
        Check.Equal(Describe(new Hero(20, "epee")), "en danger avec une epee", "un motif peut tester plusieurs proprietes");
        Check.Equal(Describe(new Hero(20, "arc")), "joueur a 20 pv", "sinon on retombe sur le motif large");
        Check.Equal(Describe(new Chest(0, true)), "coffre verrouille", "un motif de propriete booleenne");
        Check.Equal(Describe(new Chest(120, false)), "coffre de 120 pieces", "et sa version ouverte");
        Check.Equal(Describe(new Spike(80)), "piege mortel", "'when' ajoute une condition libre");
        Check.Equal(Describe(new Spike(5)), "piege", "sans laquelle on prend le cas suivant");
        Check.Equal(Describe(null), "rien", "null est un motif comme un autre");

        Check.Equal(Rank(-1), "triche", "un motif relatif");
        Check.Equal(Rank(0), "zero", "une constante");
        Check.Equal(Rank(99), "debutant", "les motifs sont testes dans l'ordre");
        Check.Equal(Rank(100), "confirme", "donc pas besoin de repeter la borne basse");
        Check.Equal(Rank(50000), "legende", "et '_' ramasse le reste");

        Check.Equal(Combo(Array.Empty<int>()), "rien", "un motif de liste vide");
        Check.Equal(Combo(new[] { 1 }), "coup simple", "une liste d'un seul element");
        Check.Equal(Combo(new[] { 1, 1, 2 }), "combo final", "une suite exacte");
        Check.Equal(Combo(new[] { 1, 9, 9, 3 }), "termine par un coup lourd", "'..' avale ce qu'il y a au milieu");
        Check.Equal(Combo(new[] { 4, 5 }), "suite libre", "et le reste tombe dans le cas par defaut");

        Check.True(TryLoot(new Chest(30, false), out int gold), "un coffre ouvert se pille");
        Check.Equal(gold, 30, "et rend son or");
        Check.False(TryLoot(new Chest(30, true), out int locked), "un coffre ferme, non");
        Check.Equal(locked, 0, "et ne rend rien");
        Check.False(TryLoot(new Spike(1), out _), "un piege non plus");
    }
}
