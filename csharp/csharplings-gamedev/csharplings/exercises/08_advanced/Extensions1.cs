namespace Csharplings;

public static class GameExtensions
{
    public static Vector2 WithY(Vector2 value, float y) => new(value.X, y);

    public static Vector2 SnappedTo(this Vector2 value, float step) =>
        new(Mathf.Snapped(value.X, step), value.Y);

    public static bool IsNullOrEmpty<T>(this ICollection<T> items) => items.Count == 0;

    public static T LastOr<T>(this IReadOnlyList<T> items, T fallback)
    {
        return Todo.Value<T>();
    }

    public static int Clamped(this int value, int min, int max) => value;
}

public static class Extensions1
{
    public const bool NotDone = true;

    public static void Run()
    {
        var position = new Vector2(120f, 33f);

        Check.Near(position.WithY(0f), new Vector2(120f, 0f), "une copie avec un seul champ change");
        Check.Near(position, new Vector2(120f, 33f), "l'original ne bouge pas");
        Check.Near(position.SnappedTo(32f), new Vector2(128f, 32f), "on colle la position sur la grille de 32");

        List<int> empty = new();
        List<int> scores = new() { 10, 40, 25 };
        List<int> missing = null;

        Check.True(empty.IsNullOrEmpty(), "une liste vide est vide");
        Check.True(missing.IsNullOrEmpty(), "une extension s'appelle meme sur null, une methode d'instance non");
        Check.False(scores.IsNullOrEmpty(), "celle-ci contient des choses");

        Check.Equal(scores.LastOr(-1), 25, "le dernier score");
        Check.Equal(empty.LastOr(-1), -1, "sinon la valeur de repli");

        Check.Equal(150.Clamped(0, 100), 100, "on peut etendre jusqu'au type int");
        Check.Equal((-5).Clamped(0, 100), 0, "des deux cotes");
        Check.Equal(42.Clamped(0, 100), 42, "et ne rien changer au milieu");
    }
}
