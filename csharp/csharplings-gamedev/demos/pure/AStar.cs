namespace Demos.Pure;

public sealed class MinHeap
{
    private int[] _nodes;
    private int[] _priorities;

    public MinHeap(int capacity)
    {
        _nodes = new int[Math.Max(capacity, 4)];
        _priorities = new int[Math.Max(capacity, 4)];
    }

    public int Count { get; private set; }

    public void Clear() => Count = 0;

    public void Push(int node, int priority)
    {
        if (Count == _nodes.Length)
        {
            Array.Resize(ref _nodes, Count * 2);
            Array.Resize(ref _priorities, Count * 2);
        }

        int slot = Count++;

        _nodes[slot] = node;
        _priorities[slot] = priority;

        while (slot > 0)
        {
            int parent = (slot - 1) >> 1;

            if (_priorities[parent] <= _priorities[slot])
                break;

            Swap(parent, slot);
            slot = parent;
        }
    }

    public int Pop()
    {
        int best = _nodes[0];

        Count--;
        _nodes[0] = _nodes[Count];
        _priorities[0] = _priorities[Count];

        int slot = 0;

        while (true)
        {
            int left = slot * 2 + 1;
            int right = left + 1;
            int smallest = slot;

            if (left < Count && _priorities[left] < _priorities[smallest])
                smallest = left;

            if (right < Count && _priorities[right] < _priorities[smallest])
                smallest = right;

            if (smallest == slot)
                break;

            Swap(smallest, slot);
            slot = smallest;
        }

        return best;
    }

    private void Swap(int a, int b)
    {
        (_nodes[a], _nodes[b]) = (_nodes[b], _nodes[a]);
        (_priorities[a], _priorities[b]) = (_priorities[b], _priorities[a]);
    }
}

public sealed class CostGrid
{
    private readonly int[] _costs;

    public CostGrid(int width, int height)
    {
        Width = width;
        Height = height;
        _costs = new int[width * height];

        Array.Fill(_costs, 1);
    }

    public int Width { get; }

    public int Height { get; }

    public int NodeCount => _costs.Length;

    public int IndexOf(int x, int y) => y * Width + x;

    public int XOf(int index) => index % Width;

    public int YOf(int index) => index / Width;

    public int CostAt(int index) => _costs[index];

    public bool IsWalkable(int index) => _costs[index] > 0;

    public void SetWall(int x, int y) => _costs[IndexOf(x, y)] = 0;

    public void SetCost(int x, int y, int cost) => _costs[IndexOf(x, y)] = cost;
}

public enum Search
{
    Dijkstra,
    AStar,
    AStarTieBroken,
}

public sealed class PathFinder
{
    private readonly CostGrid _grid;
    private readonly MinHeap _open;
    private readonly int[] _cameFrom;
    private readonly int[] _costSoFar;
    private readonly int[] _visitedStamp;
    private int _stamp;

    public PathFinder(CostGrid grid)
    {
        _grid = grid;
        _open = new MinHeap(grid.NodeCount);
        _cameFrom = new int[grid.NodeCount];
        _costSoFar = new int[grid.NodeCount];
        _visitedStamp = new int[grid.NodeCount];
    }

    public int NodesExpanded { get; private set; }

    public bool TryFindPath(int start, int goal, List<int> path, Search mode)
    {
        path.Clear();
        _open.Clear();
        _stamp++;
        NodesExpanded = 0;

        _costSoFar[start] = 0;
        _cameFrom[start] = start;
        _visitedStamp[start] = _stamp;
        _open.Push(start, 0);

        while (_open.Count > 0)
        {
            int current = _open.Pop();

            NodesExpanded++;

            if (current == goal)
                return Rebuild(start, goal, path);

            int x = _grid.XOf(current);
            int y = _grid.YOf(current);

            TryVisit(current, x + 1, y, goal, mode);
            TryVisit(current, x - 1, y, goal, mode);
            TryVisit(current, x, y + 1, goal, mode);
            TryVisit(current, x, y - 1, goal, mode);
        }

        return false;
    }

    private void TryVisit(int from, int x, int y, int goal, Search mode)
    {
        if (x < 0 || y < 0 || x >= _grid.Width || y >= _grid.Height)
            return;

        int next = _grid.IndexOf(x, y);

        if (!_grid.IsWalkable(next))
            return;

        int cost = _costSoFar[from] + _grid.CostAt(next);

        if (_visitedStamp[next] == _stamp && cost >= _costSoFar[next])
            return;

        _visitedStamp[next] = _stamp;
        _costSoFar[next] = cost;
        _cameFrom[next] = from;

        int heuristic = Manhattan(next, goal);

        int priority = mode switch
        {
            Search.Dijkstra => cost << 10,
            Search.AStar => (cost + heuristic) << 10,
            _ => ((cost + heuristic) << 10) - heuristic,
        };

        _open.Push(next, priority);
    }

    private int Manhattan(int from, int to) =>
        Math.Abs(_grid.XOf(from) - _grid.XOf(to)) + Math.Abs(_grid.YOf(from) - _grid.YOf(to));

    private bool Rebuild(int start, int goal, List<int> path)
    {
        int walk = goal;

        while (walk != start)
        {
            path.Add(walk);
            walk = _cameFrom[walk];
        }

        path.Add(start);
        path.Reverse();

        return true;
    }
}

public static class AStarDemo
{
    public static void Demo()
    {
        Console.WriteLine("--- A* : l'heuristique paie exactement autant qu'elle est bien informee ---");

        var open = new CostGrid(20, 12);

        Compare("terrain degage", open, open.IndexOf(0, 6), open.IndexOf(19, 6));

        var maze = new CostGrid(20, 12);

        for (int y = 2; y < 10; y++)
            maze.SetWall(8, y);

        for (int y = 4; y < 12; y++)
            maze.SetWall(14, y);

        for (int y = 5; y < 8; y++)
        {
            for (int x = 2; x < 6; x++)
                maze.SetCost(x, y, 6);
        }

        List<int> path = Compare("murs et terrain couteux", maze, maze.IndexOf(0, 6), maze.IndexOf(19, 6));

        Console.WriteLine();
        Print(maze, path, maze.IndexOf(0, 6), maze.IndexOf(19, 6));

        Console.WriteLine("  # mur    ~ terrain couteux (6 au lieu de 1)    o chemin    S depart    G arrivee");
        Console.WriteLine();
        Console.WriteLine("  Manhattan suppose qu'on peut aller tout droit. Sur un terrain degage c'est vrai :");
        Console.WriteLine("  204 cases visitees deviennent 20, soit exactement la longueur du chemin. A* ne");
        Console.WriteLine("  regarde plus rien d'autre que le couloir.");
        Console.WriteLine("  Des qu'un mur force un grand detour, la meme estimation devient beaucoup trop");
        Console.WriteLine("  optimiste, et A* retombe a 170 sur 209 : presque Dijkstra. Une heuristique ne vaut");
        Console.WriteLine("  que ce qu'elle sait du terrain, et celle-la ne sait rien des obstacles.");
        Console.WriteLine("  C'est pour ca qu'un jeu a couloirs precalcule un graphe de zones au lieu de lancer");
        Console.WriteLine("  A* sur la grille brute.");
        Console.WriteLine("  Casser les egalites, au passage, ne change rien ici et coute meme sept cases de");
        Console.WriteLine("  plus : ca aide sur les grandes grilles ouvertes, pas partout. A mesurer, jamais a");
        Console.WriteLine("  supposer.");
        Console.WriteLine();
    }

    private static List<int> Compare(string label, CostGrid grid, int start, int goal)
    {
        var finder = new PathFinder(grid);
        var path = new List<int>();

        finder.TryFindPath(start, goal, path, Search.Dijkstra);

        int dijkstraNodes = finder.NodesExpanded;
        int dijkstraLength = path.Count;

        finder.TryFindPath(start, goal, path, Search.AStar);

        int astarNodes = finder.NodesExpanded;
        int astarLength = path.Count;

        finder.TryFindPath(start, goal, path, Search.AStarTieBroken);

        Console.WriteLine($"  {label} ({grid.NodeCount} cases au total)");
        Console.WriteLine($"    Dijkstra          : {dijkstraNodes,3} cases visitees, chemin de {dijkstraLength}");
        Console.WriteLine($"    A*                : {astarNodes,3} cases visitees, chemin de {astarLength}");
        Console.WriteLine($"    A* egalites cassees : {finder.NodesExpanded,3} cases visitees, chemin de {path.Count}");
        Console.WriteLine($"    memes longueurs ? {dijkstraLength == astarLength && astarLength == path.Count}");

        return path;
    }

    private static void Print(CostGrid grid, List<int> path, int start, int goal)
    {
        var onPath = new HashSet<int>(path);

        for (int y = 0; y < grid.Height; y++)
        {
            Console.Write("  ");

            for (int x = 0; x < grid.Width; x++)
            {
                int index = grid.IndexOf(x, y);

                if (index == start)
                    Console.Write('S');
                else if (index == goal)
                    Console.Write('G');
                else if (!grid.IsWalkable(index))
                    Console.Write('#');
                else if (onPath.Contains(index))
                    Console.Write('o');
                else if (grid.CostAt(index) > 1)
                    Console.Write('~');
                else
                    Console.Write('.');
            }

            Console.WriteLine();
        }

        Console.WriteLine();
    }
}
