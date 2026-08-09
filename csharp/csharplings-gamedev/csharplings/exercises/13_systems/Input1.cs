namespace Csharplings;

public sealed class JumpController
{
    private const float Never = 999f;

    private float _sinceJumpPressed = Never;
    private float _sinceLeftGround = Never;

    public float BufferWindow { get; set; } = 0.12f;
    public float CoyoteWindow { get; set; } = 0.1f;

    public int Jumps { get; private set; }

    public void Tick(float delta, bool grounded, bool jumpPressed)
    {
        _sinceJumpPressed = jumpPressed ? 0f : _sinceJumpPressed + delta;
        _sinceLeftGround = grounded ? 0f : _sinceLeftGround + delta;

        if (!jumpPressed || !grounded)
            return;

        Jumps++;
    }
}

public static class Input1
{
    public const bool NotDone = true;

    public static void Run()
    {
        var simple = new JumpController();

        simple.Tick(0.02f, grounded: true, jumpPressed: true);

        Check.Equal(simple.Jumps, 1, "au sol, une pression fait sauter");

        simple.Tick(0.02f, grounded: true, jumpPressed: false);
        simple.Tick(0.02f, grounded: true, jumpPressed: false);

        Check.Equal(simple.Jumps, 1, "une pression ne doit pas relancer un saut a chaque frame");

        var buffered = new JumpController();

        for (int i = 0; i < 10; i++)
            buffered.Tick(0.04f, grounded: false, jumpPressed: false);

        buffered.Tick(0.04f, grounded: false, jumpPressed: true);

        Check.Equal(buffered.Jumps, 0, "en pleine chute, appuyer ne fait rien tout de suite");

        buffered.Tick(0.04f, grounded: true, jumpPressed: false);

        Check.Equal(buffered.Jumps, 1, "mais l'appui est garde : a l'atterrissage, le saut part");

        var late = new JumpController();

        for (int i = 0; i < 10; i++)
            late.Tick(0.04f, grounded: false, jumpPressed: false);

        late.Tick(0.04f, grounded: false, jumpPressed: true);

        for (int i = 0; i < 10; i++)
            late.Tick(0.04f, grounded: false, jumpPressed: false);

        late.Tick(0.04f, grounded: true, jumpPressed: false);

        Check.Equal(late.Jumps, 0, "un appui trop vieux est oublie : sinon le personnage saute tout seul");

        var coyote = new JumpController();

        coyote.Tick(0.04f, grounded: true, jumpPressed: false);
        coyote.Tick(0.04f, grounded: false, jumpPressed: false);
        coyote.Tick(0.04f, grounded: false, jumpPressed: true);

        Check.Equal(coyote.Jumps, 1, "juste apres le bord, on a encore droit au saut");

        var tooLate = new JumpController();

        tooLate.Tick(0.04f, grounded: true, jumpPressed: false);

        for (int i = 0; i < 5; i++)
            tooLate.Tick(0.04f, grounded: false, jumpPressed: false);

        tooLate.Tick(0.04f, grounded: false, jumpPressed: true);

        Check.Equal(tooLate.Jumps, 0, "mais un quart de seconde plus tard, on est vraiment dans le vide");
    }
}
