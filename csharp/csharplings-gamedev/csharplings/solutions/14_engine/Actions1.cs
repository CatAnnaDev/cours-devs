namespace Csharplings;

public sealed class InputState
{
    private readonly HashSet<string> _current = new();
    private readonly HashSet<string> _previous = new();

    public void Poll(params string[] held)
    {
        _previous.Clear();

        foreach (string action in _current)
            _previous.Add(action);

        _current.Clear();

        foreach (string action in held)
            _current.Add(action);
    }

    public bool IsPressed(string action) => _current.Contains(action);

    public bool IsJustPressed(string action) => _current.Contains(action) && !_previous.Contains(action);

    public bool IsJustReleased(string action) => !_current.Contains(action) && _previous.Contains(action);

    public Vector2 MoveVector()
    {
        var direction = new Vector2(
            (IsPressed("droite") ? 1f : 0f) - (IsPressed("gauche") ? 1f : 0f),
            (IsPressed("bas") ? 1f : 0f) - (IsPressed("haut") ? 1f : 0f));

        return direction.Normalized();
    }
}

public static class Actions1
{
    public const bool NotDone = false;

    public static void Run()
    {
        var input = new InputState();

        input.Poll("saut");

        Check.True(input.IsPressed("saut"), "la touche est enfoncee");
        Check.True(input.IsJustPressed("saut"), "et ca vient de se produire");
        Check.False(input.IsJustReleased("saut"), "elle n'est pas relachee");

        input.Poll("saut");

        Check.True(input.IsPressed("saut"), "elle est toujours enfoncee");
        Check.False(input.IsJustPressed("saut"),
            "mais ce n'est plus un appui neuf : sans cette distinction, tenir la touche tire en rafale");

        input.Poll();

        Check.False(input.IsPressed("saut"), "relachee");
        Check.True(input.IsJustReleased("saut"), "et on peut reagir au relachement, pour un saut a hauteur variable");

        input.Poll();

        Check.False(input.IsJustReleased("saut"), "une seule frame, pas toutes les suivantes");

        Check.Near(input.MoveVector(), Vector2.Zero, "sans touche, aucune direction");

        input.Poll("droite");

        Check.Near(input.MoveVector(), new Vector2(1f, 0f), "une seule touche : direction de longueur 1");

        input.Poll("haut");

        Check.Near(input.MoveVector(), new Vector2(0f, -1f), "en Y ecran, le haut est negatif");

        input.Poll("droite", "gauche");

        Check.Near(input.MoveVector(), Vector2.Zero, "deux touches opposees s'annulent");

        input.Poll("droite", "bas");

        Check.Near(input.MoveVector().Length(), 1.0, "en diagonale AUSSI la longueur vaut 1");
        Check.Near(input.MoveVector(), new Vector2(0.7071f, 0.7071f),
            "sinon on avance 41 pour cent plus vite en diagonale : le bug de debutant le plus repandu");
    }
}
