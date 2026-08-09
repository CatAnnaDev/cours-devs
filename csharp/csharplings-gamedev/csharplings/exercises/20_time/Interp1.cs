namespace Csharplings;

public sealed class FixedClock
{
    private float _accumulator;

    public FixedClock(float step)
    {
        Step = step;
    }

    public float Step { get; }

    public float Alpha => _accumulator;

    public int Advance(float delta)
    {
        _accumulator += delta;

        int steps = 0;

        while (_accumulator >= Step)
        {
            _accumulator -= Step;
            steps++;
        }

        return steps;
    }
}

public sealed class InterpolatedBody
{
    private Vector2 _previous;
    private Vector2 _current;

    public InterpolatedBody(Vector2 position)
    {
        _previous = position;
        _current = position;
    }

    public Vector2 Physics => _current;

    public void PhysicsStep(Vector2 velocity, float step)
    {
        _current += velocity * step;
        _previous = _current;
    }

    public void Teleport(Vector2 position)
    {
        _current = position;
    }

    public Vector2 Rendered(float alpha) => _previous.Lerp(_current, alpha);
}

public static class Interp1
{
    public const bool NotDone = true;

    private const float Step = 1f / 64f;
    private const float RenderDelta = 1f / 256f;

    public static void Run()
    {
        var body = new InterpolatedBody(Vector2.Zero);
        var velocity = new Vector2(64f, 0f);

        body.PhysicsStep(velocity, Step);

        Check.Near(body.Physics, new Vector2(1f, 0f), "un pas de physique avance d'un pixel");
        Check.Near(body.Rendered(0f), Vector2.Zero, "a alpha 0 on affiche la position d'AVANT le pas");
        Check.Near(body.Rendered(1f), new Vector2(1f, 0f), "a alpha 1 on affiche la position courante");
        Check.Near(body.Rendered(0.5f), new Vector2(0.5f, 0f), "et entre les deux, on est entre les deux");

        body.PhysicsStep(velocity, Step);

        Check.Near(body.Rendered(0f), new Vector2(1f, 0f),
            "apres un deuxieme pas, c'est le premier qui devient le point de depart : on ne garde que deux etats");

        Check.Sequence(RenderPass(interpolate: false), new[] { 0f, 0f, 0f, 1f, 1f, 1f, 1f, 2f },
            "sans interpolation, quatre frames de rendu affichent la MEME position, puis ca saute d'un pixel : c'est le saccadement");
        Check.Sequence(RenderPass(interpolate: true), new[] { 0f, 0f, 0f, 0f, 0.25f, 0.5f, 0.75f, 1f },
            "avec interpolation, chaque frame de rendu montre une position differente");

        var teleported = new InterpolatedBody(Vector2.Zero);

        teleported.PhysicsStep(velocity, Step);
        teleported.PhysicsStep(velocity, Step);
        teleported.Teleport(new Vector2(500f, 0f));

        Check.Near(teleported.Rendered(0f), new Vector2(500f, 0f),
            "apres un teleport il faut REMETTRE les deux etats a la nouvelle position");
        Check.Near(teleported.Rendered(0.5f), new Vector2(500f, 0f),
            "sinon l'objet traverse l'ecran en glissant pendant une frame, et ca se voit enormement");
        Check.Near(teleported.Physics, new Vector2(500f, 0f), "la physique, elle, est bien arrivee");

        teleported.PhysicsStep(velocity, Step);

        Check.Near(teleported.Rendered(0f), new Vector2(500f, 0f), "et le pas suivant repart proprement de la");
        Check.Near(teleported.Rendered(1f), new Vector2(501f, 0f), "vers la nouvelle position");
    }

    private static List<float> RenderPass(bool interpolate)
    {
        var body = new InterpolatedBody(Vector2.Zero);
        var clock = new FixedClock(Step);
        var velocity = new Vector2(64f, 0f);
        var samples = new List<float>();

        for (int frame = 0; frame < 8; frame++)
        {
            int steps = clock.Advance(RenderDelta);

            for (int i = 0; i < steps; i++)
                body.PhysicsStep(velocity, clock.Step);

            samples.Add(interpolate ? body.Rendered(clock.Alpha).X : body.Physics.X);
        }

        return samples;
    }
}
