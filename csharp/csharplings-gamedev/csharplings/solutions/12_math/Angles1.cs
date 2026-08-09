namespace Csharplings;

public static class Angles1
{
    public const bool NotDone = false;

    public static float AimAt(Vector2 from, Vector2 to)
    {
        return (to - from).Angle();
    }

    public static float TurnToward(float current, float target, float maxStep)
    {
        float difference = Mathf.AngleDifference(current, target);

        return current + Mathf.Clamp(difference, -maxStep, maxStep);
    }

    public static bool IsInCone(Vector2 origin, float facing, Vector2 target, float halfAngle)
    {
        float toTarget = (target - origin).Angle();

        return Mathf.Abs(Mathf.AngleDifference(facing, toTarget)) <= halfAngle;
    }

    public static int Facing8(float radians)
    {
        return Mathf.PosMod(Mathf.RoundToInt(radians / (Mathf.Tau / 8f)), 8);
    }

    public static void Run()
    {
        Check.Near(Mathf.RadToDeg(AimAt(Vector2.Zero, new Vector2(10f, 0f))), 0.0, "viser a droite : angle zero", 0.01);
        Check.Near(Mathf.RadToDeg(AimAt(Vector2.Zero, new Vector2(0f, 10f))), 90.0, "en Y ecran, 90 degres pointe vers le bas", 0.01);
        Check.Near(Mathf.RadToDeg(AimAt(new Vector2(5f, 5f), new Vector2(5f, 0f))), -90.0, "et -90 vers le haut", 0.01);

        Check.Near(Mathf.RadToDeg(TurnToward(0f, Mathf.DegToRad(10f), Mathf.DegToRad(30f))), 10.0,
            "si la cible est proche, on l'atteint d'un coup", 0.01);
        Check.Near(Mathf.RadToDeg(TurnToward(0f, Mathf.DegToRad(170f), Mathf.DegToRad(30f))), 30.0,
            "sinon on avance d'un pas au maximum", 0.01);

        float wrapped = TurnToward(Mathf.DegToRad(350f), Mathf.DegToRad(10f), Mathf.DegToRad(30f));

        Check.Near(Mathf.RadToDeg(Mathf.Wrap(wrapped, 0f, Mathf.Tau)), 10.0,
            "de 350 vers 10 il n'y a que 20 degres : il ne faut PAS repasser par 180", 0.01);

        Check.Near(Mathf.RadToDeg(TurnToward(Mathf.DegToRad(10f), Mathf.DegToRad(350f), Mathf.DegToRad(5f))), 5.0,
            "et dans l'autre sens on tourne aussi par le plus court chemin", 0.01);

        Check.True(IsInCone(Vector2.Zero, 0f, new Vector2(10f, 1f), Mathf.DegToRad(45f)), "presque devant : vu");
        Check.True(IsInCone(Vector2.Zero, 0f, new Vector2(10f, -9f), Mathf.DegToRad(45f)), "en diagonale haute : vu aussi");
        Check.False(IsInCone(Vector2.Zero, 0f, new Vector2(0f, 10f), Mathf.DegToRad(45f)), "sur le cote : hors du cone");
        Check.False(IsInCone(Vector2.Zero, 0f, new Vector2(-10f, 0f), Mathf.DegToRad(45f)), "dans le dos : hors du cone");
        Check.True(IsInCone(Vector2.Zero, Mathf.Pi, new Vector2(-10f, 0f), Mathf.DegToRad(45f)), "sauf si le garde regarde par la");

        Check.Equal(Facing8(0f), 0, "l'animation de droite");
        Check.Equal(Facing8(Mathf.DegToRad(45f)), 1, "la suivante dans le sens des aiguilles");
        Check.Equal(Facing8(Mathf.DegToRad(180f)), 4, "celle de gauche");
        Check.Equal(Facing8(Mathf.DegToRad(-45f)), 7, "et un angle negatif retombe dans 0..7");
        Check.Equal(Facing8(Mathf.DegToRad(359f)), 0, "juste avant le tour complet, on est revenu a la premiere");
    }
}
