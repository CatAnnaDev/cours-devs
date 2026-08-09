namespace Csharplings;

public static class Twice1
{
    public const bool NotDone = false;

    public static int Enumerations;

    public static IEnumerable<int> Source(int count)
    {
        Enumerations++;

        for (int i = 0; i < count; i++)
            yield return i;
    }

    public static string Describe(IEnumerable<int> scores)
    {
        List<int> materialized = scores as List<int> ?? scores.ToList();

        if (materialized.Count == 0)
            return "vide";

        return $"{materialized.Count} scores, meilleur {materialized.Max()}";
    }

    public static IEnumerable<int> Drain(IEnumerator<int> cursor)
    {
        while (cursor.MoveNext())
            yield return cursor.Current;
    }

    public static void Run()
    {
        Enumerations = 0;

        IEnumerable<int> scores = Source(4);

        Check.Equal(Describe(scores), "4 scores, meilleur 3", "la description est correcte");
        Check.Equal(Enumerations, 1,
            "et elle n'a parcouru la source qu'UNE fois : Count puis Max sur un IEnumerable, ce sont deux parcours");

        Enumerations = 0;
        IEnumerable<int> twice = Source(4);
        int naive = twice.Count() + twice.Max();

        Check.Equal(naive, 7, "la version naive donne le meme resultat");
        Check.Equal(Enumerations, 2,
            "en payant deux fois. Sur une requete qui lit un fichier ou traverse une grille, c'est le double de tout");

        var list = new List<int> { 1, 2, 3 };

        Check.Equal(Describe(list), "3 scores, meilleur 3",
            "quand la source EST deja une liste, on ne la recopie pas : le test 'as List' evite une allocation dans le cas frequent");

        Check.True(list.TryGetNonEnumeratedCount(out int known) && known == 3,
            "TryGetNonEnumeratedCount dit si on peut connaitre la taille SANS parcourir : vrai pour une List, un tableau, un Dictionary");

        Check.False(Source(3).TryGetNonEnumeratedCount(out _),
            "faux pour un iterateur : il n'y a personne a qui demander, il faudrait derouler");

        IEnumerator<int> cursor = new List<int> { 7, 8, 9 }.GetEnumerator();
        IEnumerable<int> once = Drain(cursor);

        Check.Sequence(once, new[] { 7, 8, 9 }, "certaines sources ne sont parcourables qu'une seule fois");

        Check.Sequence(once, Array.Empty<int>(),
            "le second parcours rend le VIDE, sans erreur : un flux consomme ne se rembobine pas, et c'est le cas d'un fichier, d'une socket ou d'un curseur de base");

        Check.Equal(Describe(Array.Empty<int>()), "vide", "et un cas vide doit se traiter apres materialisation");

        Check.Throws<InvalidOperationException>(() => Array.Empty<int>().Max(),
            "parce que Max sur une sequence vide leve : demander le maximum de rien n'a pas de reponse");
    }
}
