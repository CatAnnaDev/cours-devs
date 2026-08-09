namespace Csharplings;

public sealed class SpatialHash
{
    private readonly Dictionary<(int X, int Y), List<int>> _cells = new();
    private readonly float _cellSize;

    public SpatialHash(float cellSize)
    {
        _cellSize = cellSize;
    }

    public int CellCount => _cells.Count;

    public (int X, int Y) CellOf(Vector2 position) =>
        ((int)(position.X / _cellSize), (int)(position.Y / _cellSize));

    public void Clear() => _cells.Clear();

    public void Insert(int id, Vector2 position)
    {
        (int X, int Y) cell = CellOf(position);

        _cells[cell] = new List<int> { id };
    }

    public List<int> Query(Vector2 center, float radius)
    {
        var found = new List<int>();

        if (_cells.TryGetValue(CellOf(center), out List<int> bucket))
            found.AddRange(bucket);

        return found;
    }
}

public static class Spatial1
{
    public const bool NotDone = true;

    public static void Run()
    {
        var grid = new SpatialHash(64f);

        Check.Equal(grid.CellOf(new Vector2(0f, 0f)), (0, 0), "l'origine tombe dans la case 0,0");
        Check.Equal(grid.CellOf(new Vector2(70f, 130f)), (1, 2), "une position se range par division entiere");
        Check.Equal(grid.CellOf(new Vector2(-1f, -1f)), (-1, -1), "les coordonnees negatives descendent, elles ne tronquent pas");

        var positions = new List<Vector2>();

        for (int i = 0; i < 1000; i++)
            positions.Add(new Vector2(i % 100 * 40f, i / 100 * 40f));

        for (int i = 0; i < positions.Count; i++)
            grid.Insert(i, positions[i]);

        Check.True(grid.CellCount > 20, "mille entites se repartissent sur beaucoup de cases");

        List<int> candidates = grid.Query(positions[0], 50f);

        Check.True(candidates.Count < 100, "au lieu de tester 1000 paires, on en teste une poignee");
        Check.True(candidates.Contains(0), "et l'entite cherchee est forcement dans le lot");
        Check.Equal(candidates.Count, candidates.Distinct().Count(), "une entite ne doit jamais sortir deux fois");

        var neighbours = new List<int>();

        foreach (int id in candidates)
        {
            if (positions[id].DistanceTo(positions[0]) <= 50f)
                neighbours.Add(id);
        }

        var bruteForce = new List<int>();

        for (int i = 0; i < positions.Count; i++)
        {
            if (positions[i].DistanceTo(positions[0]) <= 50f)
                bruteForce.Add(i);
        }

        Check.Sequence(neighbours.OrderBy(id => id), bruteForce.OrderBy(id => id),
            "le filtrage grossier ne doit oublier personne : meme resultat qu'en testant tout");

        grid.Clear();

        Check.Equal(grid.CellCount, 0, "on vide la grille a chaque frame");
        Check.Equal(grid.Query(Vector2.Zero, 1000f).Count, 0, "et une grille vide ne propose rien");
    }
}
