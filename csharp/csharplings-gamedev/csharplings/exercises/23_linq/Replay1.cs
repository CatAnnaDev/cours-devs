namespace Csharplings;

public static class Replay1
{
    public const bool NotDone = true;

    public static int Calls;

    public static bool IsAlive(int health)
    {
        Calls++;

        return health > 0;
    }

    public static IEnumerable<int> Alive(List<int> healths) => healths.Where(IsAlive).ToList();

    public static List<int> AliveOnce(List<int> healths) => Todo.Value<List<int>>();

    public static void Run()
    {
        var healths = new List<int> { 10, 0, 30 };

        Calls = 0;
        IEnumerable<int> query = Alive(healths);

        Check.Equal(Calls, 0,
            "declarer une requete n'execute RIEN : ce que tu tiens est une recette, pas un resultat");

        Check.Equal(query.Count(), 2, "c'est le parcours qui declenche le travail");
        Check.Equal(Calls, 3, "trois appels au filtre pour trois elements");

        Check.Equal(query.Count(), 2, "reparcourir la meme requete redonne le meme resultat");
        Check.Equal(Calls, 6,
            "mais en REFAISANT tout le travail : six appels pour deux parcours. C'est le bug de performance le plus discret du C#");

        Calls = 0;
        List<int> frozen = AliveOnce(healths);

        Check.Equal(Calls, 3, "ToList execute une fois");
        Check.Equal(frozen.Count, 2, "et rend une vraie liste");

        Calls = 0;
        Check.Equal(frozen.Count, 2, "qu'on peut relire autant qu'on veut");
        Check.Equal(Calls, 0, "sans rejouer quoi que ce soit");

        healths.Add(50);

        Check.Equal(query.Count(), 3,
            "la requete voit l'element ajoute APRES sa declaration : elle pointe la source, elle ne l'a pas copiee");

        Check.Equal(frozen.Count, 2, "la liste figee, elle, ne bouge plus : c'est tout l'interet de figer");

        var volatileSource = new List<int> { 10, 0, 30 };
        IEnumerable<int> overVolatile = volatileSource.Where(IsAlive);

        Check.Throws<InvalidOperationException>(
            () =>
            {
                foreach (int health in overVolatile)
                    volatileSource.Add(health);
            },
            "et modifier la source PENDANT le parcours leve une exception : la requete lit la liste au fur et a mesure, elle ne l'a jamais copiee");

        Calls = 0;
        int count = query.Count();
        int first = query.First();

        Check.Equal(count, 3, "trois vivants");
        Check.Equal(first, 10, "et le premier est le premier");
        Check.True(Calls > 3,
            $"mais compter puis prendre le premier a coute {Calls} appels au lieu de 4 : deux parcours pour deux questions. Une seule materialisation aurait suffi");

        Calls = 0;
        List<int> once = query.ToList();

        Check.Equal(once.Count, 3, "materialiser une fois");
        Check.Equal(once[0], 10, "puis poser toutes ses questions a la liste");
        Check.Equal(Calls, 4, "quatre appels, un par element, et c'est tout");
    }
}
