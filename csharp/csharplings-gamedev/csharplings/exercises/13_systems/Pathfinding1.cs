namespace Csharplings;

public sealed class GridMap
{
    private readonly string[] _rows;

    public GridMap(params string[] rows)
    {
        _rows = rows;
    }

    public int Height => _rows.Length;
    public int Width => _rows[0].Length;

    public bool IsWalkable(int x, int y) => _rows[y][x] != '#';
}

public static class Pathfinding1
{
    public const bool NotDone = true;

    private static readonly (int X, int Y)[] Neighbours = { (1, 0), (-1, 0), (0, 1), (0, -1) };

    public static List<(int X, int Y)> FindPath(GridMap map, (int X, int Y) start, (int X, int Y) goal)
    {
        return Todo.Value<List<(int X, int Y)>>();
    }

    public static void Run()
    {
        var detour = new GridMap(
            "..........",
            "########..",
            "..........");

        List<(int X, int Y)> path = FindPath(detour, (0, 0), (0, 2));

        Check.Equal(path.Count, 19, "le mur force un detour par la droite : 19 cases");
        Check.Equal(path[0], (0, 0), "le chemin commence au depart");
        Check.Equal(path[path.Count - 1], (0, 2), "et finit a l'arrivee");

        for (int i = 1; i < path.Count; i++)
        {
            int distance = Mathf.Abs(path[i].X - path[i - 1].X) + Mathf.Abs(path[i].Y - path[i - 1].Y);

            if (distance != 1)
                throw new CheckFailedException($"deux cases du chemin ne se touchent pas : {path[i - 1]} et {path[i]}");

            if (!detour.IsWalkable(path[i].X, path[i].Y))
                throw new CheckFailedException($"le chemin traverse un mur en {path[i]}");
        }

        Check.True(true, "chaque case du chemin touche la precedente et reste sur du sol");

        Check.Equal(FindPath(detour, (3, 0), (3, 0)).Count, 1, "aller la ou on est deja, c'est un chemin d'une case");

        var sealedOff = new GridMap(
            "....",
            "####",
            "....");

        Check.Equal(FindPath(sealedOff, (0, 0), (0, 2)).Count, 0, "sans passage, on rend un chemin vide");
        Check.Equal(FindPath(detour, (0, 0), (0, 1)).Count, 0, "et on ne vise pas une case pleine");
        Check.Equal(FindPath(detour, (0, 0), (99, 99)).Count, 0, "ni une case hors de la carte");
    }
}
