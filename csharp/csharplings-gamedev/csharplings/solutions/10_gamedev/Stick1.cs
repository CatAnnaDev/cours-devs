namespace Csharplings;

public static class Stick1
{
    public const bool NotDone = false;

    public const float Deadzone = 0.25f;

    public static Vector2 Axial(Vector2 raw)
    {
        float x = Mathf.Abs(raw.X) < Deadzone ? 0f : raw.X;
        float y = Mathf.Abs(raw.Y) < Deadzone ? 0f : raw.Y;

        return new Vector2(x, y);
    }

    public static Vector2 Radial(Vector2 raw)
    {
        float length = raw.Length();

        if (length < Deadzone)
            return Vector2.Zero;

        float scaled = (length - Deadzone) / (1f - Deadzone);

        return raw / length * Mathf.Min(scaled, 1f);
    }

    public static Vector2 Curved(Vector2 raw, float exponent)
    {
        Vector2 clean = Radial(raw);
        float length = clean.Length();

        if (Mathf.IsZeroApprox(length))
            return Vector2.Zero;

        return clean / length * Mathf.Pow(length, exponent);
    }

    public static void Run()
    {
        Check.Near(Radial(new Vector2(0.1f, 0.1f)), Vector2.Zero,
            "sous la zone morte, le stick est considere au repos : sans ca, un stick use fait deriver le personnage tout seul");

        Check.Near(Radial(new Vector2(1f, 0f)), new Vector2(1f, 0f), "a fond dans un axe, on rend un vecteur unitaire");

        Check.Near(Radial(new Vector2(Deadzone + 0.001f, 0f)), Vector2.Zero,
            "et juste au-dessus du seuil, on repart de ZERO : c'est le REMAPPAGE. Sans lui, franchir la zone morte fait sauter la vitesse de 0 a 0.25 d'un coup, et le personnage part en sursaut", 0.01);

        Check.Near(Radial(new Vector2(0.625f, 0f)).Length(), 0.5f, "et le milieu de la course utile donne bien un demi", 0.001);

        var diagonal = new Vector2(0.2f, 0.2f);

        Check.Near(Axial(diagonal), Vector2.Zero, "en zone morte AXIALE, une petite diagonale est annulee comme il faut");

        var lopsided = new Vector2(0.3f, 0.2f);

        Check.Near(Axial(lopsided), new Vector2(0.3f, 0f),
            "mais celle-ci devient purement HORIZONTALE : la composante verticale passe sous le seuil et disparait. Le joueur pousse en diagonale et le personnage part tout droit");

        Check.True(Radial(lopsided).Y > 0f,
            "la zone morte RADIALE regarde la longueur du vecteur, pas ses composantes : la diagonale reste une diagonale");

        Check.Near(Radial(lopsided).Normalized(), lopsided.Normalized(),
            "et la DIRECTION est preservee exactement : c'est la seule chose que le joueur controle vraiment");

        Check.True(Radial(new Vector2(1.05f, 0.3f)).Length() <= 1f,
            "un stick lit parfois plus de 1 dans les coins. Sans plafond, le personnage court plus vite en diagonale que droit devant, et les speedrunners le trouvent en une heure");

        Vector2 gentle = Curved(new Vector2(0.5f, 0f), exponent: 2f);
        Vector2 linear = Radial(new Vector2(0.5f, 0f));

        Check.True(gentle.Length() < linear.Length(),
            "une courbe de reponse ecrase le bas de la course : plus de precision pour viser, sans rien perdre en haut");

        Check.Near(Curved(new Vector2(1f, 0f), 2f).Length(), 1f, "parce que la pleine course reste la pleine course : un exposant ne touche pas au maximum", 0.001);

        Check.Near(Curved(new Vector2(0.5f, 0f), 1f), Radial(new Vector2(0.5f, 0f)), "et un exposant de 1 rend la reponse lineaire");

        Check.Near(Curved(Vector2.Zero, 2f), Vector2.Zero,
            "dernier detail, et il plante tout le monde une fois : normaliser un vecteur NUL divise par zero. Il faut tester la longueur avant");
    }
}
