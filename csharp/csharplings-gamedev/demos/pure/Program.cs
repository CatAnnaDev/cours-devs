using Demos.Pure;

public static class Program
{
    private static readonly (string Name, Action Run)[] Demos =
    {
        ("rng", Rng.Demo),
        ("ring", RingBufferDemo.Demo),
        ("fixed", FixedPointDemo.Demo),
        ("tree", BehaviourTreeDemo.Demo),
        ("astar", AStarDemo.Demo),
        ("weak", WeakRefsDemo.Demo),
    };

    public static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] is "list" or "--list")
        {
            Console.WriteLine("  demos disponibles :");

            foreach ((string name, _) in Demos)
                Console.WriteLine($"    {name}");

            return 0;
        }

        if (args.Length > 0)
        {
            foreach (string wanted in args)
            {
                (string Name, Action Run) found = Demos.FirstOrDefault(demo => demo.Name == wanted);

                if (found.Run is null)
                {
                    Console.WriteLine($"  demo inconnue : {wanted}");

                    return 1;
                }

                found.Run();
            }

            return 0;
        }

        foreach ((_, Action run) in Demos)
            run();

        return 0;
    }
}
