namespace Csharplings;

public sealed class GameState : Node
{
    public static GameState Instance { get; private set; }

    public int Score { get; set; }

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (ReferenceEquals(Instance, this))
            Instance = null;
    }
}

public static class ScoreMath
{
    public static int Combo(int hits, int multiplier)
    {
        return hits * multiplier;
    }
}

public static class Godot5
{
    public const bool NotDone = false;

    public static void Run()
    {
        Check.Equal(ScoreMath.Combo(3, 10), 30, "un calcul pur n'a pas besoin d'instance");

        Check.True(GameState.Instance is null, "avant l'entree dans l'arbre, il n'y a pas d'instance");

        var tree = new SceneTree();
        var state = new GameState { Name = "GameState" };
        tree.Root.AddChild(state);
        tree.Start();

        Check.True(GameState.Instance is not null, "l'instance se pose des l'entree dans l'arbre");
        Check.True(ReferenceEquals(GameState.Instance, state), "et c'est bien le noeud qu'on a ajoute");

        GameState.Instance.Score = 250;
        Check.Equal(state.Score, 250, "le score vit dans l'instance, pas dans une variable static");

        state.QueueFree();
        tree.Tick();

        Check.True(GameState.Instance is null, "a la sortie de l'arbre, on remet l'instance a null");
    }
}
