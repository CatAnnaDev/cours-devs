namespace Csharplings;

public static class Slide1
{
    public const bool NotDone = false;

    public const float MaxSlopeDegrees = 45f;

    public static Vector2 Slide(Vector2 velocity, Vector2 normal) =>
        velocity - normal * velocity.Dot(normal);

    public static Vector2 Bounce(Vector2 velocity, Vector2 normal, float bounciness) =>
        velocity - normal * velocity.Dot(normal) * (1f + bounciness);

    public static bool IsFloor(Vector2 normal) =>
        normal.Dot(Vector2.Up) >= Mathf.Cos(Mathf.DegToRad(MaxSlopeDegrees));

    public static Vector2 SlideAll(Vector2 velocity, Vector2[] normals)
    {
        Vector2 result = velocity;

        foreach (Vector2 normal in normals)
        {
            if (result.Dot(normal) < 0f)
                result = Slide(result, normal);
        }

        return result;
    }

    public static void Run()
    {
        Check.Near(Slide(new Vector2(10f, 10f), Vector2.Up), new Vector2(10f, 0f),
            "contre le sol : on annule la composante verticale et on GARDE l'horizontale. S'arreter net, c'est le bug");

        Check.Near(Slide(new Vector2(10f, 5f), Vector2.Left), new Vector2(0f, 5f),
            "contre un mur a droite : on annule l'horizontale et on continue de descendre");

        Vector2 downhill = Vector2.Up.Rotated(Mathf.DegToRad(45f));

        Check.Near(Slide(new Vector2(10f, 0f), downhill), new Vector2(5f, 5f),
            "sur une pente a 45 degres, avancer vers la droite fait aussi descendre : la direction change");
        Check.Near(Slide(new Vector2(10f, 0f), downhill).Length(), 7.071,
            "et la vitesse baisse : glisser, c'est perdre la part du mouvement qui rentrait dans la surface", 0.001);

        Check.Near(Bounce(new Vector2(10f, 10f), Vector2.Up, 0f), Slide(new Vector2(10f, 10f), Vector2.Up),
            "un rebond sans elasticite, c'est exactement un glissement");
        Check.Near(Bounce(new Vector2(10f, 10f), Vector2.Up, 1f), new Vector2(10f, -10f),
            "avec une elasticite de 1, la composante normale est INVERSEE au lieu d'etre annulee");
        Check.Near(Bounce(new Vector2(10f, 10f), Vector2.Up, 0.5f), new Vector2(10f, -5f),
            "et entre les deux, on en rend la moitie");

        Check.True(IsFloor(Vector2.Up), "une surface horizontale est un sol");
        Check.True(IsFloor(Vector2.Up.Rotated(Mathf.DegToRad(44f))), "une pente a 44 degres aussi, on peut la monter");
        Check.False(IsFloor(Vector2.Up.Rotated(Mathf.DegToRad(46f))),
            "a 46 degres, non : au-dela de la pente maximale on glisse au lieu de marcher");
        Check.False(IsFloor(Vector2.Left), "un mur n'est pas un sol");
        Check.False(IsFloor(Vector2.Down), "et un plafond encore moins");

        Check.Near(SlideAll(new Vector2(10f, 10f), new[] { Vector2.Up }), new Vector2(10f, 0f),
            "un seul contact, un seul glissement");

        Check.Near(SlideAll(new Vector2(10f, 10f), new[] { Vector2.Up, Vector2.Left }), Vector2.Zero,
            "dans un coin, sol puis mur : il ne reste plus rien du mouvement, et c'est correct");

        Check.Near(SlideAll(new Vector2(-10f, 10f), new[] { Vector2.Left }), new Vector2(-10f, 10f),
            "et surtout : s'ELOIGNER d'un mur ne doit rien annuler du tout. C'est le test qui manque le plus souvent, et il colle le joueur aux surfaces qu'il vient de quitter");

        Check.Near(SlideAll(new Vector2(10f, -10f), new[] { Vector2.Up }), new Vector2(10f, -10f),
            "sauter depuis le sol non plus : le produit scalaire est positif, on ne touche plus");

        Vector2 gravity = new Vector2(0f, 100f);
        Vector2 steep = Vector2.Up.Rotated(Mathf.DegToRad(60f));

        Check.False(IsFloor(steep), "une pente a 60 degres est trop raide");
        Check.True(Slide(gravity, steep).X > 0f,
            "la gravite glisse donc vers le bas de la pente : c'est comme ca qu'un personnage derape sur une paroi trop raide");
        Check.Near(Slide(gravity, steep).Length(), 86.602,
            "et il ne garde que la part de la gravite parallele a la pente", 0.01);
    }
}
