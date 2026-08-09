namespace Csharplings;

public static class Yield1
{
    public const bool NotDone = true;

    public static int Pulled;

    public static IEnumerable<T> TakeUntil<T>(this IEnumerable<T> source, Func<T, bool> stop)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        if (stop is null)
            throw new ArgumentNullException(nameof(stop));

        foreach (T item in source)
        {
            yield return item;

            if (stop(item))
                yield break;
        }
    }

    public static IEnumerable<List<T>> Batch<T>(this IEnumerable<T> source, int size)
    {
        var batch = new List<T>(size);

        foreach (T item in source)
        {
            batch.Add(item);

            if (batch.Count < size)
                continue;

            yield return batch;

            batch.Clear();
        }

        if (batch.Count > 0)
            yield return batch;
    }

    public static IEnumerable<int> Counted(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Pulled++;

            yield return i;
        }
    }

    public static void Run()
    {
        Check.Sequence(new[] { 1, 2, 3, 9, 4, 5 }.TakeUntil(value => value > 5), new[] { 1, 2, 3, 9 },
            "un operateur a soi : on rend l'element QUI a declenche l'arret, ce que TakeWhile ne sait pas faire");

        Check.Sequence(new[] { 1, 2, 3 }.TakeUntil(value => value > 99), new[] { 1, 2, 3 },
            "et sans declencheur, on rend tout");

        Check.Throws<ArgumentNullException>(() => TakeUntil<int>(null, value => true).ToList(),
            "un argument invalide doit etre refuse");

        Check.Throws<ArgumentNullException>(() => TakeUntil<int>(null, value => true),
            "et refuse TOUT DE SUITE, pas au premier parcours. Une methode qui contient un 'yield' ne s'execute qu'au premier MoveNext, verification des arguments comprise : il faut donc une methode d'entree SANS yield qui verifie, puis delegue a l'iterateur");

        Check.Equal(new[] { 1, 2, 3, 4, 5 }.Batch(2).Count(), 3, "decouper en paquets de deux donne trois paquets");
        Check.Sequence(new[] { 1, 2, 3, 4, 5 }.Batch(2).Last(), new[] { 5 }, "le dernier est incomplet, et il sort quand meme");
        Check.Sequence(new[] { 1, 2, 3, 4 }.Batch(2).First(), new[] { 1, 2 }, "les autres sont pleins");

        Pulled = 0;
        IEnumerable<int> lazy = Counted(1000);

        Check.Equal(Pulled, 0, "'yield' donne l'execution differee gratuitement : rien ne tourne avant le parcours");

        Check.Equal(lazy.First(), 0, "prendre le premier");
        Check.Equal(Pulled, 1, "ne produit QUE le premier : la machine a etats s'arrete a chaque yield et attend");

        Pulled = 0;
        Check.Equal(lazy.Take(3).Count(), 3, "en prendre trois");
        Check.Equal(Pulled, 3, "en produit trois. Les 997 autres n'ont jamais existe");

        Pulled = 0;
        Check.Equal(lazy.Count(), 1000, "et tout compter les produit tous");
        Check.Equal(Pulled, 1000, "un par un, sans jamais avoir mille entiers en memoire en meme temps");

        var batches = new[] { 1, 2, 3, 4 }.Batch(2).ToList();

        Check.Equal(batches.Count, 2, "attention tout de meme : materialiser les paquets marche ici");
        Check.Sequence(batches[0], new[] { 1, 2 }, "parce qu'on fabrique une NOUVELLE liste par paquet");
        Check.Sequence(batches[1], new[] { 3, 4 },
            "recycler le meme tampon serait plus economique, mais les paquets deja rendus changeraient dans le dos de l'appelant");
    }
}
