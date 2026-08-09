namespace Csharplings;

public static class Loops1
{
    public const bool NotDone = true;

    public static void RemoveDead(List<int> healths)
    {
        for (int i = 0; i < healths.Count; i++)
        {
            if (healths[i] <= 0)
                healths.RemoveAt(i);
        }
    }

    public static void Compact(List<int> healths)
    {
        for (int i = 0; i < healths.Count; i++)
        {
            if (healths[i] <= 0)
                healths.RemoveAt(i);
        }
    }

    public static void SwapRemove<T>(List<T> items, int index)
    {
        items.RemoveAt(index);
    }

    public static void Run()
    {
        var healths = new List<int> { 0, 0, 5, 0, 0 };
        RemoveDead(healths);

        Check.Sequence(healths, new[] { 5 },
            "en descendant de la fin vers zero, retirer un element ne decale pas ceux qu'on n'a pas encore vus");

        var trailing = new List<int> { 3, 0, 0 };
        RemoveDead(trailing);

        Check.Sequence(trailing, new[] { 3 }, "y compris quand les morts sont a la fin");

        var nothing = new List<int>();
        RemoveDead(nothing);

        Check.Equal(nothing.Count, 0, "une liste vide ne doit pas planter");

        var same = new List<int> { 0, 0, 5, 0, 0 };
        Compact(same);

        Check.Sequence(same, new[] { 5 }, "RemoveAll fait la meme chose en une ligne, et en un seul passage");

        var letters = new List<string> { "a", "b", "c", "d" };
        SwapRemove(letters, 1);

        Check.Equal(letters.Count, 3, "un element de moins");
        Check.False(letters.Contains("b"), "et c'est bien celui qu'on visait");
        Check.Sequence(letters, new[] { "a", "d", "c" },
            "le dernier vient boucher le trou : l'ordre saute, mais on ne decale pas 10 000 elements");

        var single = new List<string> { "seul" };
        SwapRemove(single, 0);

        Check.Equal(single.Count, 0, "retirer le dernier element doit marcher aussi");

        var live = new List<int> { 1, 0, 2 };

        Check.Throws<InvalidOperationException>(
            () =>
            {
                foreach (int health in live)
                {
                    if (health <= 0)
                        live.Remove(health);
                }
            },
            "modifier une liste pendant un foreach leve une exception, c'est le rappel a l'ordre du runtime");

        Check.Equal(live.Count, 2, "et la liste est laissee dans un etat a moitie modifie : raison de plus pour ne pas faire ca");
    }
}
