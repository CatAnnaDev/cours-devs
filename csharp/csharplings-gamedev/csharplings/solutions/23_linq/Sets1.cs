namespace Csharplings;

public sealed record Tag(string Name);

public sealed class Marker
{
    public Marker(string name)
    {
        Name = name;
    }

    public string Name { get; }
}

public static class Sets1
{
    public const bool NotDone = false;

    public static List<string> NewlyUnlocked(List<string> now, List<string> before) =>
        now.Except(before).ToList();

    public static List<string> Common(List<string> left, List<string> right) =>
        left.Intersect(right).ToList();

    public static List<string> All(List<string> left, List<string> right) =>
        left.Union(right).ToList();

    public static void Run()
    {
        Check.Equal(new[] { new Tag("feu"), new Tag("feu"), new Tag("eau") }.Distinct().Count(), 2,
            "Distinct passe par Equals et GetHashCode. Un record les fabrique a partir de ses champs, donc deux tags identiques n'en font qu'un");

        Check.Equal(new[] { new Marker("feu"), new Marker("feu") }.Distinct().Count(), 2,
            "une classe ordinaire, non : son egalite est celle des REFERENCES, et deux objets distincts restent distincts quoi qu'ils contiennent");

        Check.Equal(new[] { new Marker("feu"), new Marker("feu") }.DistinctBy(marker => marker.Name).Count(), 1,
            "DistinctBy demande la cle explicitement : plus court qu'un IEqualityComparer, et ca marche sur les types qu'on ne peut pas modifier");

        var before = new List<string> { "saut", "double_saut" };
        var now = new List<string> { "saut", "double_saut", "dash", "dash" };

        Check.Sequence(NewlyUnlocked(now, before), new[] { "dash" },
            "Except rend ce qui est a gauche et pas a droite. Et il DEDOUBLONNE au passage : c'est une operation d'ensembles, pas de listes");

        Check.Sequence(Common(now, before), new[] { "saut", "double_saut" }, "Intersect rend ce qui est des deux cotes");
        Check.Sequence(All(before, now), new[] { "saut", "double_saut", "dash" }, "Union rend l'un et l'autre, sans doublon");

        Check.True(new[] { 1, 2, 3 }.SequenceEqual(new[] { 1, 2, 3 }),
            "SequenceEqual compare element par element, dans l'ordre : ce n'est pas une comparaison d'ensembles");

        Check.False(new[] { 1, 2, 3 }.SequenceEqual(new[] { 3, 2, 1 }), "le meme contenu dans un autre ordre n'est pas egal");

        Check.True(new HashSet<int> { 1, 2, 3 }.SetEquals(new[] { 3, 2, 1 }),
            "c'est SetEquals qui compare des ensembles, et il se moque de l'ordre comme des doublons");

        var many = Enumerable.Range(0, 5000).ToList();
        var lookup = many.ToHashSet();

        Check.True(many.Contains(4999), "Contains sur une List parcourt jusqu'a trouver : au pire, toute la liste");
        Check.True(lookup.Contains(4999), "sur un HashSet, il calcule une empreinte et va droit au but");

        Check.Equal(many.Intersect(new[] { 4999, -1 }).Count(), 1,
            "et Intersect construit un HashSet de son cote avant de comparer : n plus m, jamais n fois m");

        var seen = new HashSet<string>();

        Check.True(seen.Add("boss"), "Add rend true la premiere fois");
        Check.False(seen.Add("boss"),
            "et false ensuite : un HashSet est la facon la plus courte d'ecrire 'ne fais ceci qu'une fois par cible'");
    }
}
