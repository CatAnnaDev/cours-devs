namespace Csharplings;

public static class Movement1
{
    public const bool NotDone = false;

    public const float Gravity = 980f;
    public const float TerminalVelocity = 600f;

    public static float JumpVelocity(float desiredHeight)
    {
        return -Mathf.Sqrt(2f * Gravity * desiredHeight);
    }

    public static float ApplyGravity(float verticalVelocity, float delta)
    {
        return Mathf.Min(verticalVelocity + Gravity * delta, TerminalVelocity);
    }

    public static float Accelerate(float velocity, float input, float acceleration, float friction, float maxSpeed, float delta)
    {
        if (Mathf.IsZeroApprox(input))
            return Mathf.MoveToward(velocity, 0f, friction * delta);

        return Mathf.MoveToward(velocity, input * maxSpeed, acceleration * delta);
    }

    public static void Run()
    {
        float jump = JumpVelocity(64f);

        Check.True(jump < 0f, "en Y ecran, sauter c'est aller vers les Y negatifs");
        Check.Near(jump * jump / (2f * Gravity), 64.0, "la hauteur atteinte se deduit de la vitesse initiale", 0.01);

        float velocity = jump;
        float height = 0f;
        float peak = 0f;
        const float step = 1f / 1000f;

        while (velocity < 0f)
        {
            velocity = ApplyGravity(velocity, step);
            height += velocity * step;
            peak = Mathf.Min(peak, height);
        }

        Check.Near(peak, -64.0, "et la simulation monte bien a la hauteur demandee", 0.5);

        float falling = 0f;

        for (int i = 0; i < 600; i++)
            falling = ApplyGravity(falling, 1f / 60f);

        Check.Near(falling, 600.0, "dix secondes de chute : on plafonne a la vitesse terminale");
        Check.True(falling <= TerminalVelocity, "sans quoi on traverse les murs a la premiere chute un peu longue");

        Check.Near(Accelerate(0f, 1f, 2000f, 1000f, 300f, 0.1f), 200.0, "on accelere de 2000 par seconde");
        Check.Near(Accelerate(200f, 1f, 2000f, 1000f, 300f, 0.1f), 300.0, "et on s'arrete pile a la vitesse maximale");
        Check.Near(Accelerate(300f, 1f, 2000f, 1000f, 300f, 0.1f), 300.0, "sans jamais la depasser");

        Check.Near(Accelerate(300f, 0f, 2000f, 1000f, 300f, 0.1f), 200.0, "sans input, la friction freine");
        Check.Near(Accelerate(50f, 0f, 2000f, 1000f, 300f, 1f), 0.0,
            "et un gros pas de friction s'arrete a zero : il ne fait pas repartir en arriere");

        Check.Near(Accelerate(300f, -1f, 2000f, 1000f, 300f, 0.1f), 100.0, "faire demi-tour passe par zero, ca ne claque pas");
    }
}
