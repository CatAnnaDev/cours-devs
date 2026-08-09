namespace Csharplings;

public sealed class Walker : Node
{
    public float Speed { get; set; } = 100f;

    public override void _Process(double delta)
    {
        Position = Position + Vector2.Right * Speed;
    }
}

public static class Godot2
{
    public const bool NotDone = true;

    private static float DistanceAfter(int frames, double delta)
    {
        var tree = new SceneTree();
        var walker = new Walker { Name = "Walker" };

        tree.Root.AddChild(walker);
        tree.Start();
        tree.Run(frames, delta);

        return walker.Position.X;
    }

    public static void Run()
    {
        Check.Near(DistanceAfter(60, 1.0 / 60.0), 100.0, "a 60 images par seconde, 100 pixels en 1 seconde", 0.5);
        Check.Near(DistanceAfter(30, 1.0 / 30.0), 100.0, "a 30 images par seconde, la MEME distance", 0.5);
        Check.Near(DistanceAfter(144, 1.0 / 144.0), 100.0, "a 144 images par seconde, toujours pareil", 0.5);
    }
}
