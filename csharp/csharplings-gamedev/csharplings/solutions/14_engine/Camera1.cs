namespace Csharplings;

public static class Camera1
{
    public const bool NotDone = false;

    public static Vector2 FollowWithDeadzone(Vector2 camera, Vector2 target, float deadzone)
    {
        Vector2 offset = target - camera;
        float distance = offset.Length();

        if (distance <= deadzone)
            return camera;

        return camera + offset.Normalized() * (distance - deadzone);
    }

    public static Vector2 ClampToLevel(Vector2 camera, Rect2 level, Vector2 viewport)
    {
        Vector2 half = viewport / 2f;

        float x = level.Size.X <= viewport.X
            ? level.Center.X
            : Mathf.Clamp(camera.X, level.Position.X + half.X, level.End.X - half.X);

        float y = level.Size.Y <= viewport.Y
            ? level.Center.Y
            : Mathf.Clamp(camera.Y, level.Position.Y + half.Y, level.End.Y - half.Y);

        return new Vector2(x, y);
    }

    public static float DecayTrauma(float trauma, float decay, float delta)
    {
        return Mathf.Max(trauma - decay * delta, 0f);
    }

    public static float ShakeAmount(float trauma, float maxOffset)
    {
        return maxOffset * trauma * trauma;
    }

    public static void Run()
    {
        var camera = Vector2.Zero;

        Check.Near(FollowWithDeadzone(camera, new Vector2(20f, 0f), 30f), camera,
            "dans la zone morte, la camera ne bouge pas : sinon elle tremble sur chaque pas du joueur");

        Vector2 moved = FollowWithDeadzone(camera, new Vector2(100f, 0f), 30f);

        Check.Near(moved, new Vector2(70f, 0f), "au dela, elle rattrape juste ce qu'il faut");
        Check.Near(moved.DistanceTo(new Vector2(100f, 0f)), 30.0, "et le joueur se retrouve pile au bord de la zone morte");

        Check.Near(FollowWithDeadzone(camera, camera, 30f), camera, "une cible confondue avec la camera ne doit pas diviser par zero");

        var level = new Rect2(0f, 0f, 1000f, 1000f);
        var viewport = new Vector2(320f, 180f);

        Check.Near(ClampToLevel(Vector2.Zero, level, viewport), new Vector2(160f, 90f),
            "on ne montre jamais le vide au bord gauche du niveau");
        Check.Near(ClampToLevel(new Vector2(5000f, 5000f), level, viewport), new Vector2(840f, 910f),
            "ni au bord droit");
        Check.Near(ClampToLevel(new Vector2(500f, 500f), level, viewport), new Vector2(500f, 500f),
            "au milieu, on ne touche a rien");

        var tiny = new Rect2(0f, 0f, 200f, 100f);

        Check.Near(ClampToLevel(new Vector2(999f, 999f), tiny, viewport), new Vector2(100f, 50f),
            "un niveau plus petit que l'ecran se centre au lieu de coincer les bornes");

        Check.Near(DecayTrauma(1f, 2f, 0.1f), 0.8, "le traumatisme redescend lineairement");
        Check.Near(DecayTrauma(0.05f, 2f, 0.1f), 0.0, "et s'arrete a zero, jamais dans le negatif");

        Check.Near(ShakeAmount(1f, 20f), 20.0, "a fond, la secousse vaut le maximum");
        Check.Near(ShakeAmount(0.5f, 20f), 5.0,
            "a la moitie elle vaut le QUART : le carre fait mourir la secousse en douceur au lieu de couper net");
        Check.Near(ShakeAmount(0f, 20f), 0.0, "et sans traumatisme, plus rien");
    }
}
