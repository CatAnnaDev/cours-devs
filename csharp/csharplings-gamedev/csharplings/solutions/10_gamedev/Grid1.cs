namespace Csharplings;

public static class Grid1
{
    public const bool NotDone = false;

    public const float CellSize = 32f;

    public static Vector2 CellToWorld(int column, int row)
    {
        return new Vector2(column * CellSize, row * CellSize);
    }

    public static (int Column, int Row) WorldToCell(Vector2 world)
    {
        return (Mathf.FloorToInt(world.X / CellSize), Mathf.FloorToInt(world.Y / CellSize));
    }

    public static int CellToIndex(int column, int row, int width)
    {
        return row * width + column;
    }

    public static bool IsInside(int column, int row, int width, int height)
    {
        return column >= 0 && row >= 0 && column < width && row < height;
    }

    public static void Run()
    {
        Vector2 world = CellToWorld(3, 2);
        Check.Near(world.X, 96.0, "la colonne 3 commence a 96 pixels");
        Check.Near(world.Y, 64.0, "la ligne 2 commence a 64 pixels");

        Check.Equal(WorldToCell(new Vector2(100f, 70f)), (3, 2), "100,70 tombe dans la case 3,2");
        Check.Equal(WorldToCell(new Vector2(0f, 0f)), (0, 0), "l'origine est la case 0,0");
        Check.Equal(WorldToCell(new Vector2(-1f, -1f)), (-1, -1), "juste a gauche de l'origine : case -1,-1");
        Check.Equal(WorldToCell(new Vector2(-40f, 0f)), (-2, 0), "un cast tronque vers zero, il ne descend pas");

        Check.Equal(CellToIndex(0, 0, 10), 0, "la premiere case d'un tableau plat");
        Check.Equal(CellToIndex(3, 0, 10), 3, "quatrieme colonne, premiere ligne");
        Check.Equal(CellToIndex(3, 2, 10), 23, "sur une grille large de 10 : 2 lignes puis 3 colonnes");

        Check.True(IsInside(0, 0, 10, 10), "le coin haut gauche est dans la grille");
        Check.True(IsInside(9, 9, 10, 10), "le coin bas droit aussi");
        Check.False(IsInside(10, 0, 10, 10), "la colonne 10 est deja dehors");
        Check.False(IsInside(-1, 0, 10, 10), "et une colonne negative aussi");
    }
}
