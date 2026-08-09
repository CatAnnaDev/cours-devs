namespace Csharplings;

public static class Marshal1
{
    public const bool NotDone = true;

    public static int SumThroughConversion(GodotArray engineSide)
    {
        List<int> managed = engineSide.ToIntList();
        int total = 0;

        for (int i = 0; i < managed.Count; i++)
            total += managed[i];

        return total;
    }

    public static int SumInPlace(GodotArray engineSide)
    {
        List<int> managed = engineSide.ToIntList();
        int total = 0;

        foreach (int value in managed)
            total += value;

        return total;
    }

    public static void Run()
    {
        var managed = new List<int> { 1, 2, 3 };

        GodotArray.ResetCounter();

        GodotArray engineSide = GodotArray.FromIntList(managed);

        Check.Equal(GodotArray.Conversions, 1, "passer une liste C# au moteur, c'est une conversion");
        Check.Equal(engineSide.Count, 3, "les trois elements sont arrives");
        Check.Equal(engineSide[0].AsInt(), 1, "avec les bonnes valeurs");

        managed[0] = 99;

        Check.Equal(engineSide[0].AsInt(), 1,
            "et la conversion a COPIE : modifier la liste C# ne change rien au tableau moteur. Ce ne sont pas deux vues sur la meme memoire");

        List<int> back = engineSide.ToIntList();

        Check.Equal(GodotArray.Conversions, 2, "le retour est une conversion de plus, et une copie de plus");
        Check.Equal(back[0], 1, "on recupere l'etat du cote moteur, pas celui de la liste d'origine");

        back[0] = 42;

        Check.Equal(engineSide[0].AsInt(), 1, "et la copie de retour est independante elle aussi");

        var big = new List<int>(1000);

        for (int i = 0; i < 1000; i++)
            big.Add(i);

        GodotArray converted = GodotArray.FromIntList(big);

        GodotArray.ResetCounter();

        Check.Equal(SumThroughConversion(converted), SumInPlace(converted),
            "les deux facons de sommer donnent le meme total");
        Check.Equal(SumInPlace(converted), 499_500, "la somme de 0 a 999");

        Check.Equal(GodotArray.Conversions, 1,
            "mais l'une des deux a converti pour rien : elle a recopie mille elements avant de les additionner");

        long converting = Report("sommer 1000 elements en reconvertissant", Allocations(() => SumThroughConversion(converted)));
        long inPlace = Report("sommer les memes en lisant le tableau moteur", Allocations(() => SumInPlace(converted)));

        Check.True(converting > 4_000L,
            "une conversion de mille entiers, c'est une liste neuve et son tableau interne : quatre kilo-octets, pour rien");
        Check.Equal(inPlace, 0L,
            "lire le tableau moteur element par element n'alloue rien. Plus lent par element, mais gratuit en memoire");

        Check.True(converting > inPlace,
            "la regle : on convertit UNE fois, au chargement, et on garde la forme dont on a besoin. Jamais dans une boucle de jeu");

        GodotArray.ResetCounter();

        var accumulated = new GodotArray();

        for (int i = 0; i < 10; i++)
            accumulated.Add(i);

        Check.Equal(accumulated.Count, 10, "on peut aussi remplir le tableau moteur directement");
        Check.Equal(GodotArray.Conversions, 0, "et la, aucune conversion du tout");
    }

    private static long Allocations(Action action)
    {
        action();
        action();
        action();

        long before = GC.GetAllocatedBytesForCurrentThread();
        action();

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static long Report(string what, long bytes)
    {
        Console.WriteLine($"      mesure  {what} : {bytes} octets");

        return bytes;
    }
}
