namespace Csharplings;

public sealed class Sprite : Node
{
}

public sealed class Character : Node
{
    public Sprite Body { get; private set; }
    public Node MissingOnPurpose { get; private set; }

    public override void _Ready()
    {
        Body = GetNode<Sprite>("Body");
        MissingOnPurpose = GetNode<Node>("Chapeau");
    }
}

public static class Godot3
{
    public const bool NotDone = true;

    public static string SafeName(Node node)
    {
        return node.Name;
    }

    public static void Run()
    {
        var tree = new SceneTree();
        var character = new Character { Name = "Player" };
        character.AddChild(new Sprite { Name = "Body" });
        tree.Root.AddChild(character);
        tree.Start();

        Check.True(character.Body is not null, "le noeud Body a ete trouve dans _Ready");
        Check.Equal(character.Body.Name, "Body", "et c'est le bon");
        Check.True(character.MissingOnPurpose is null, "un noeud absent doit donner null, pas une exception");

        var doomed = new Node { Name = "Temporaire" };
        tree.Root.AddChild(doomed);
        tree.Start();

        Check.Equal(SafeName(doomed), "Temporaire", "tant qu'il est vivant on lit son nom");

        doomed.QueueFree();
        tree.Tick();

        Check.Equal(SafeName(doomed), "libere", "apres QueueFree, IsInstanceValid est la seule verite");
        Check.Equal(SafeName(null), "libere", "et null doit passer aussi");
    }
}
