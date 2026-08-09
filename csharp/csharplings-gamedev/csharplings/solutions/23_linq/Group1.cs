namespace Csharplings;

public sealed record Drop(string Zone, string Item, int Weight);

public sealed record Zone(string Name, int Level);

public static class Group1
{
    public const bool NotDone = false;

    private static readonly List<Drop> Drops = new()
    {
        new Drop("cave", "torche", 30),
        new Drop("cave", "corde", 20),
        new Drop("foret", "baie", 50),
        new Drop("cave", "gemme", 5),
        new Drop("foret", "bois", 40),
    };

    private static readonly List<Zone> Zones = new()
    {
        new Zone("cave", 3),
        new Zone("foret", 1),
        new Zone("volcan", 12),
    };

    public static ILookup<string, Drop> ByZone() => Drops.ToLookup(drop => drop.Zone);

    public static List<string> ItemsOfZones(IEnumerable<string> zones) =>
        zones.SelectMany(zone => ByZone()[zone]).Select(drop => drop.Item).ToList();

    public static List<string> Reachable(int maxLevel) =>
        Zones
            .Where(zone => zone.Level <= maxLevel)
            .Join(Drops, zone => zone.Name, drop => drop.Zone, (zone, drop) => drop.Item)
            .ToList();

    public static Dictionary<string, int> WeightPerZone() =>
        Zones.GroupJoin(Drops, zone => zone.Name, drop => drop.Zone, (zone, drops) => (zone.Name, Total: drops.Sum(drop => drop.Weight)))
            .ToDictionary(pair => pair.Name, pair => pair.Total);

    public static void Run()
    {
        ILookup<string, Drop> byZone = ByZone();

        Check.Equal(byZone["cave"].Count(), 3, "un lookup range les elements par cle");
        Check.Equal(byZone["volcan"].Count(), 0,
            "et une cle absente rend une sequence VIDE au lieu de lever : c'est toute la difference avec un Dictionary, et ce qui evite un TryGetValue a chaque acces");

        Check.Sequence(byZone.Select(group => group.Key), new[] { "cave", "foret" },
            "les groupes sortent dans l'ordre de PREMIERE apparition de la cle, pas dans l'ordre alphabetique");

        int calls = 0;
        IEnumerable<IGrouping<string, Drop>> grouped = Drops.GroupBy(drop => { calls++; return drop.Zone; });

        Check.Equal(calls, 0, "GroupBy est differe : rien n'est calcule tant qu'on ne parcourt pas");

        calls = 0;
        Drops.ToLookup(drop => { calls++; return drop.Zone; });

        Check.Equal(calls, 5,
            "ToLookup est IMMEDIAT : il construit sa table tout de suite. Deux operateurs qui font la meme chose, avec deux moments d'execution opposes");

        Check.Sequence(ItemsOfZones(new[] { "cave", "foret" }),
            new[] { "torche", "corde", "gemme", "baie", "bois" },
            "SelectMany aplatit : une sequence de sequences devient une sequence");

        Check.Sequence(Reachable(5), new[] { "torche", "corde", "gemme", "baie", "bois" },
            "un Join croise deux collections sur une cle commune");

        Check.Sequence(Reachable(2), new[] { "baie", "bois" },
            "et seules les zones qui passent le filtre apportent leurs objets");

        Dictionary<string, int> weights = WeightPerZone();

        Check.Equal(weights["cave"], 55, "GroupJoin garde chaque element de gauche avec TOUS ses correspondants a droite");
        Check.Equal(weights.Count, 3,
            "trois zones en entree, trois zones en sortie : GroupJoin ne fait disparaitre personne, meme le volcan qui ne lache rien");
        Check.Equal(weights["volcan"], 0,
            "c'est le 'left join'. Un Join ordinaire, lui, aurait purement et simplement supprime la zone sans butin");

        Check.Equal(Reachable(99).Count, 5, "un Join construit une table de hachage sur la collection de droite");
        Check.True(Zones.Count * Drops.Count > Reachable(99).Count,
            "il coute donc n plus m, la ou deux boucles imbriquees coutent n fois m. Sur mille zones et mille objets, c'est deux mille comparaisons contre un million");
    }
}
