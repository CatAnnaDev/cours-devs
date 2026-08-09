namespace Csharplings;

public sealed class FallingBox
{
    public FallingBox(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
    }

    public Vector2 Position { get; set; }

    public Vector2 Size { get; }

    public Vector2 Velocity { get; set; }

    public Rect2 Bounds => new Rect2(Position, Size);
}

public static class Resolve1
{
    public const bool NotDone = false;

    public const float Gravity = 980f;
    public const float Skin = 0.01f;

    public static Vector2 MinimumPush(Rect2 moving, Rect2 solid)
    {
        if (!moving.Intersects(solid))
            return Vector2.Zero;

        float left = solid.Position.X - moving.End.X;
        float right = solid.End.X - moving.Position.X;
        float up = solid.Position.Y - moving.End.Y;
        float down = solid.End.Y - moving.Position.Y;

        float x = Mathf.Abs(left) < Mathf.Abs(right) ? left : right;
        float y = Mathf.Abs(up) < Mathf.Abs(down) ? up : down;

        return Mathf.Abs(x) < Mathf.Abs(y) ? new Vector2(x, 0f) : new Vector2(0f, y);
    }

    public static void Resolve(FallingBox box, Rect2 solid)
    {
        Vector2 push = MinimumPush(box.Bounds, solid);

        if (push == Vector2.Zero)
            return;

        Vector2 normal = push.Normalized();

        box.Position += push + normal * Skin;

        if (box.Velocity.Dot(normal) < 0f)
            box.Velocity -= normal * box.Velocity.Dot(normal);
    }

    public static void Run()
    {
        var floor = new Rect2(0f, 100f, 200f, 20f);

        var sunk = new FallingBox(new Vector2(50f, 95f), new Vector2(10f, 10f));

        Check.True(sunk.Bounds.Intersects(floor), "la boite est enfoncee de cinq pixels dans le sol");

        Resolve(sunk, floor);

        Check.False(sunk.Bounds.Intersects(floor), "apres resolution elle n'est plus dedans");
        Check.Near(sunk.Position.Y, 90.0 - Skin,
            "elle est posee juste au-dessus, avec une marge minuscule : sans cette marge on se recolle a la frame suivante", 0.001);
        Check.Near(sunk.Position.X, 50.0, "et l'axe non concerne n'a pas bouge d'un pixel");

        var moving = new FallingBox(new Vector2(50f, 95f), new Vector2(10f, 10f))
        {
            Velocity = new Vector2(120f, 300f),
        };

        Resolve(moving, floor);

        Check.Near(moving.Velocity.Y, 0.0, "on annule la composante de vitesse qui rentrait dans le sol");
        Check.Near(moving.Velocity.X, 120.0,
            "mais on garde la composante parallele : annuler toute la vitesse collerait le joueur au sol des qu'il le touche");

        var leaving = new FallingBox(new Vector2(50f, 95f), new Vector2(10f, 10f))
        {
            Velocity = new Vector2(0f, -400f),
        };

        Resolve(leaving, floor);

        Check.Near(leaving.Velocity.Y, -400.0,
            "et une boite qui S'ELOIGNE garde sa vitesse : sans ce test, le sol mange le saut du joueur");

        var resting = new FallingBox(new Vector2(50f, 0f), new Vector2(10f, 10f));
        float lastY = resting.Position.Y;
        float jitter = 0f;
        float maxSpeed = 0f;

        for (int frame = 0; frame < 200; frame++)
        {
            resting.Velocity += new Vector2(0f, Gravity / 60f);
            resting.Position += resting.Velocity * (1f / 60f);

            Resolve(resting, floor);

            if (frame > 60)
                jitter = Mathf.Max(jitter, Mathf.Abs(resting.Position.Y - lastY));

            maxSpeed = Mathf.Max(maxSpeed, Mathf.Abs(resting.Velocity.Y));
            lastY = resting.Position.Y;
        }

        Report("tremblement de la boite posee, apres stabilisation", jitter);
        Report("vitesse verticale maximale atteinte", maxSpeed);

        Check.Near(resting.Position.Y, 90.0 - Skin,
            "apres 200 frames de gravite, la boite est posee sur le sol et pas ailleurs", 0.01);
        Check.Near(jitter, 0.0,
            "et elle ne TREMBLE pas : parce qu'on annule sa vitesse, elle repart de zero chaque frame au lieu d'accelerer", 0.001);
        Check.True(maxSpeed < Gravity,
            "sans l'annulation de vitesse elle accelererait indefiniment, finirait par traverser les vingt pixels du sol en une frame, et tomberait a travers");
        Check.True(resting.Bounds.End.Y <= floor.Position.Y + Skin * 2f,
            "elle est bien restee AU-DESSUS du sol, pas passee dessous");
    }

    private static void Report(string what, float value) =>
        Console.WriteLine($"      mesure  {what} : {value:0.######}");
}
