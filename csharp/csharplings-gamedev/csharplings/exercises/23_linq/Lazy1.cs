namespace Csharplings;

public static class Lazy1
{
    public const bool NotDone = true;

    public static int Produced;

    public static IEnumerable<int> WaveSizes()
    {
        int size = 3;

        while (true)
        {
            Produced++;

            yield return size;

            size += 1;
        }
    }

    public static IEnumerable<Vector2> SpawnRing(Vector2 center, float radius)
    {
        for (int i = 0; ; i++)
        {
            float angle = i * 0.7f;

            yield return center + Vector2.FromAngle(angle);
        }
    }

    public static IEnumerable<int> Identifiers(int start) => Todo.Value<IEnumerable<int>>();

    public static void Run()
    {
        Produced = 0;

        Check.Sequence(WaveSizes().Take(4), new[] { 3, 5, 8, 13 },
            "une suite INFINIE se declare avec while(true) et yield : ce qui la rend utilisable, c'est que personne ne la parcourt en entier");

        Check.Equal(Produced, 4, "quatre valeurs demandees, quatre valeurs produites");

        Produced = 0;

        Check.Equal(WaveSizes().First(size => size > 20), 31,
            "First s'arrete des qu'il a trouve : il ne demande jamais la valeur suivante");

        Check.Equal(Produced, 6, "six vagues examinees, et pas une de plus");

        Check.Sequence(WaveSizes().TakeWhile(size => size < 10), new[] { 3, 5, 8 },
            "TakeWhile s'arrete au premier element qui ne passe pas");

        Check.Sequence(WaveSizes().Skip(2).Take(2), new[] { 8, 13 },
            "Skip consomme sans rendre : les deux premieres vagues sont bien calculees, simplement jetees");

        Check.Sequence(Identifiers(100).Take(3), new[] { 100, 101, 102 },
            "un generateur d'identifiants tient en trois lignes et ne stocke rien");

        Check.Equal(SpawnRing(Vector2.Zero, 10f).Take(8).Count(), 8, "huit positions sur un cercle");

        Check.Near(SpawnRing(Vector2.Zero, 10f).First(), new Vector2(10f, 0f), "la premiere est a droite du centre");

        Check.True(SpawnRing(Vector2.Zero, 10f).Take(20).All(point => Mathf.IsEqualApprox(point.Length(), 10f)),
            "et toutes sont a la bonne distance, sans qu'aucun tableau n'ait jamais ete alloue");

        Check.Sequence(Enumerable.Range(1, 5).Select(step => step * step), new[] { 1, 4, 9, 16, 25 },
            "Range fabrique une suite finie sans la stocker non plus");

        Check.Sequence(Enumerable.Repeat("gobelin", 3), new[] { "gobelin", "gobelin", "gobelin" }, "Repeat aussi");

        Check.Sequence(Enumerable.Empty<int>(), Array.Empty<int>(),
            "et Empty rend une sequence vide partagee, celle qu'on renvoie au lieu de null");

        Check.Equal(WaveSizes().Where(size => size > 5).Take(2).Count(), 2,
            "un Where sur une suite infinie reste infini : c'est le Take qui met fin au parcours");

        Check.Equal(WaveSizes().Take(20).Count(size => size > 5), 18,
            "d'ou la regle : sur une source infinie, il faut TOUJOURS un operateur qui limite avant un operateur qui compte, trie ou materialise. Sinon la boucle de jeu se fige, sans exception et sans message");
    }
}
