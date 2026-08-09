namespace Csharplings;

public sealed class Box
{
    private object _content;

    public Box(object content)
    {
        _content = content;
    }

    public object Open() => _content;
}

public static class Generics1
{
    public const bool NotDone = true;

    public static object FirstOr(List<object> items, object fallback)
    {
        return items.Count > 0 ? items[0] : fallback;
    }

    public static void Run()
    {
        int firstScore = FirstOr(new List<int> { 4, 5 }, 0);
        Check.Equal(firstScore, 4, "le premier score, sans aucun cast");

        string firstName = FirstOr(new List<string>(), "personne");
        Check.Equal(firstName, "personne", "la valeur de repli sur une liste vide");

        var box = new Box<int>(42);
        int value = box.Open();
        Check.Equal(value, 42, "une boite generique rend le type exact");

        var wordBox = new Box<string>("slime");
        Check.Equal(wordBox.Open().ToUpper(), "SLIME", "et on garde toutes les methodes du type");
    }
}
