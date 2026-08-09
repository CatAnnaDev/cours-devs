namespace Csharplings;

public static class Budget1
{
    public const bool NotDone = false;

    public static bool ShouldUpdate(int index, int frame, int slices)
    {
        return Mathf.PosMod(index, slices) == Mathf.PosMod(frame, slices);
    }

    public static float UpdateInterval(float distance)
    {
        if (distance < 200f)
            return 0f;

        if (distance < 800f)
            return 0.2f;

        return 1f;
    }

    public static bool IsDue(float elapsedSinceUpdate, float interval)
    {
        return elapsedSinceUpdate >= interval;
    }

    public static void Run()
    {
        const int count = 1000;
        const int slices = 4;

        var updates = new int[count];
        var perFrame = new List<int>();

        for (int frame = 0; frame < slices; frame++)
        {
            int worked = 0;

            for (int i = 0; i < count; i++)
            {
                if (!ShouldUpdate(i, frame, slices))
                    continue;

                updates[i]++;
                worked++;
            }

            perFrame.Add(worked);
        }

        Check.Sequence(perFrame, new[] { 250, 250, 250, 250 },
            "chaque frame ne traite qu'un quart du troupeau : le cout par frame est divise par quatre");

        Check.True(updates.All(times => times == 1),
            "et sur quatre frames, chaque entite a ete traitee exactement une fois");
        Check.Equal(updates.Sum(), count, "personne n'est oublie, personne n'est traite deux fois");

        Check.True(ShouldUpdate(0, 0, 1), "avec une seule tranche, tout le monde passe a chaque frame");
        Check.True(ShouldUpdate(7, 103, 4), "le decoupage tient sur des numeros de frame quelconques");
        Check.False(ShouldUpdate(7, 104, 4), "et une entite ne passe que sur sa tranche");

        Check.Near(UpdateInterval(50f), 0.0, "tout pres du joueur : mise a jour a chaque frame");
        Check.Near(UpdateInterval(500f), 0.2, "a moyenne distance : cinq fois par seconde suffit");
        Check.Near(UpdateInterval(5000f), 1.0, "tres loin : une fois par seconde, personne ne verra la difference");

        Check.True(IsDue(0f, 0f), "un intervalle de zero veut dire chaque frame");
        Check.False(IsDue(0.1f, 0.2f), "sinon on attend");
        Check.True(IsDue(0.2f, 0.2f), "et on agit des qu'on atteint l'intervalle");

        float elapsed = 0f;
        int ticks = 0;

        for (int frame = 0; frame < 60; frame++)
        {
            elapsed += 1f / 60f;

            if (!IsDue(elapsed, UpdateInterval(500f)))
                continue;

            ticks++;
            elapsed = 0f;
        }

        Check.Equal(ticks, 5, "une seconde a moyenne distance : cinq mises a jour au lieu de soixante");
    }
}
