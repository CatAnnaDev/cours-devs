namespace Csharplings;

public sealed class Runner : Node
{
    public int PhysicsCalls { get; private set; }
    public int ProcessCalls { get; private set; }

    public Vector2 Velocity { get; set; } = new(600f, 0f);
    public Vector2 VisualPosition { get; private set; }

    public List<string> Log { get; } = new();

    public override void _Process(double delta)
    {
        ProcessCalls++;
        Log.Add("visuel");
        Position += Velocity * (float)delta;
        VisualPosition = Position;
    }
}

public sealed class FollowCamera : Node
{
    private readonly Runner _target;

    public FollowCamera(Runner target)
    {
        _target = target;
    }

    public override void _Process(double delta)
    {
        Position = _target.VisualPosition;
    }
}

public static class Order1
{
    public const bool NotDone = true;

    public static void Run()
    {
        var tree = new SceneTree();
        var runner = new Runner { Name = "Runner" };
        var camera = new FollowCamera(runner) { Name = "Camera" };

        tree.Root.AddChild(camera);
        tree.Root.AddChild(runner);
        tree.Start();

        tree.Tick(1.0 / 60.0);

        Check.Equal(runner.PhysicsCalls, 1, "une frame normale : un pas de physique");
        Check.Equal(runner.ProcessCalls, 1, "et une passe de rendu");
        Check.Equal(runner.Log[0], "physique", "la physique passe AVANT le rendu dans une frame");
        Check.Near(runner.Position, new Vector2(10f, 0f), "600 pixels par seconde pendant un soixantieme de seconde");

        Check.Near(camera.Position, new Vector2(10f, 0f),
            "la camera est ajoutee AVANT sa cible dans l'arbre, et pourtant elle ne prend pas une frame de retard");

        var slow = new SceneTree();
        var heavy = new Runner { Name = "Runner" };

        slow.Root.AddChild(heavy);
        slow.Start();
        slow.Tick(3.0 / 60.0);

        Check.Equal(heavy.PhysicsCalls, 3, "une frame trois fois trop longue : la physique rattrape en trois pas fixes");
        Check.Equal(heavy.ProcessCalls, 1, "mais on ne dessine qu'une fois : c'est ca, la difference entre les deux boucles");
        Check.Near(heavy.Position, new Vector2(30f, 0f), "et le personnage a parcouru exactement la meme distance");

        var stutter = new SceneTree();
        var walker = new Runner { Name = "Runner", Velocity = new Vector2(60f, 0f) };

        stutter.Root.AddChild(walker);
        stutter.Start();

        for (int frame = 0; frame < 120; frame++)
            stutter.Tick(1.0 / 120.0);

        Check.Equal(walker.ProcessCalls, 120, "120 images affichees");
        Check.Equal(walker.PhysicsCalls, 60, "mais seulement 60 pas de physique : le pas fixe ne suit pas les FPS");
        Check.Near(walker.Position, new Vector2(60f, 0f), "une seconde a 60 pixels par seconde, quel que soit le nombre d'images", 0.01);
    }
}
