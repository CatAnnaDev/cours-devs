using System.Runtime.InteropServices;

namespace Csharplings;

public sealed unsafe class NativeGrid : IDisposable
{
    private int* _cells;

    public NativeGrid(int width, int height)
    {
        Width = width;
        Height = height;
        _cells = (int*)NativeMemory.AllocZeroed((nuint)(width * height), (nuint)sizeof(int));
    }

    public int Width { get; }

    public int Height { get; }

    public bool Disposed => _cells is null;

    public int this[int x, int y]
    {
        get
        {
            if (_cells is null)
                throw new ObjectDisposedException(nameof(NativeGrid));

            return _cells[x * Width + y];
        }

        set
        {
            if (_cells is null)
                throw new ObjectDisposedException(nameof(NativeGrid));

            _cells[y * Width + x] = value;
        }
    }

    public Span<int> AsSpan()
    {
        if (_cells is null)
            throw new ObjectDisposedException(nameof(NativeGrid));

        return new Span<int>(_cells, Width * Height);
    }

    public void Dispose()
    {
        if (_cells is null)
            return;

        NativeMemory.Free(_cells);
    }
}

public static unsafe class Native1
{
    public const bool NotDone = true;

    public static long Measure(Action action)
    {
        action();
        action();
        action();

        long before = GC.GetAllocatedBytesForCurrentThread();
        action();

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    public static void Run()
    {
        using (var grid = new NativeGrid(4, 4))
        {
            Check.Equal(grid[0, 0], 0, "AllocZeroed rend une zone deja remise a zero");

            grid[2, 3] = 7;

            Check.Equal(grid[2, 3], 7, "on y ecrit comme dans un tableau");
            Check.Equal(grid[3, 2], 0, "et les voisins ne bougent pas : l'indexation a la main doit etre juste, personne ne la verifiera");

            Span<int> cells = grid.AsSpan();

            Check.Equal(cells.Length, 16, "un Span peut se poser SUR de la memoire native : toute l'API de Span devient disponible");

            cells.Fill(1);

            Check.Equal(grid[0, 0], 1, "et il ecrit bien dans la zone native, pas dans une copie");

            Check.Equal(Measure(() => grid.AsSpan().Fill(2)), 0L,
                "cette memoire-la est INVISIBLE du ramasse-miettes : elle ne compte dans aucune generation, elle ne declenche aucun ramassage, et elle ne bouge jamais");
        }

        var leaked = new NativeGrid(2, 2);

        Check.False(leaked.Disposed, "tant qu'on ne libere pas, la zone reste reservee");

        leaked.Dispose();

        Check.True(leaked.Disposed, "et c'est a TOI de la liberer : aucun ramasse-miettes ne viendra le faire");

        leaked.Dispose();

        Check.True(leaked.Disposed, "un Dispose appele deux fois ne doit rien casser : c'est la regle, et c'est ce qui rend le 'using' sur : il appelle Dispose meme quand une exception traverse le bloc");

        Check.Throws<ObjectDisposedException>(() => { _ = leaked[0, 0]; },
            "lire apres liberation doit lever une exception CLAIRE. Sans ce garde, on lit une zone rendue au systeme : parfois les anciennes valeurs, parfois celles d'une autre allocation, parfois un plantage du processus entier");

        void* block = NativeMemory.Alloc(64);

        Check.True(block is not null, "l'allocation brute rend un pointeur, jamais null en cas de succes");

        NativeMemory.Free(block);

        Check.True(true,
            "et la regle du jeu tient en une phrase : un Alloc, un Free, sur le meme pointeur, exactement une fois. C'est le prix a payer pour une zone qui ne bouge pas et qu'une API native peut garder entre deux images");
    }
}
