namespace Csharplings;

public sealed class Targeting
{
    public Targeting(float range, float coneDegrees, float stickiness)
    {
        RangeSquared = range * range;
        ConeCosine = Mathf.Cos(Mathf.DegToRad(coneDegrees * 0.5f));
        Stickiness = stickiness;
    }

    public float RangeSquared { get; }

    public float ConeCosine { get; }

    public float Stickiness { get; }

    public int Current { get; private set; } = -1;

    public bool IsEligible(Vector2 from, Vector2 facing, Vector2 target)
    {
        Vector2 offset = target - from;
        float distanceSquared = offset.LengthSquared();

        if (distanceSquared > RangeSquared)
            return false;

        return facing.Dot(offset.Normalized()) >= ConeCosine;
    }

    public int Pick(Vector2 from, Vector2 facing, List<Vector2> candidates)
    {
        int best = -1;
        float bestScore = float.MaxValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            if (!IsEligible(from, facing, candidates[i]))
                continue;

            float score = (candidates[i] - from).LengthSquared();

            if (score >= bestScore)
                continue;

            bestScore = score;
            best = i;
        }

        Current = best;

        return best;
    }

    public void Clear() => Current = -1;
}

public static class Target1
{
    public const bool NotDone = true;

    public static void Run()
    {
        var targeting = new Targeting(range: 100f, coneDegrees: 90f, stickiness: 0.7f);
        var facing = Vector2.Right;

        var candidates = new List<Vector2>
        {
            new Vector2(50f, 0f),
            new Vector2(20f, 0f),
            new Vector2(-30f, 0f),
            new Vector2(500f, 0f),
            new Vector2(0f, 40f),
        };

        Check.True(targeting.IsEligible(Vector2.Zero, facing, candidates[0]), "devant et a portee : eligible");
        Check.False(targeting.IsEligible(Vector2.Zero, facing, candidates[2]),
            "derriere : le produit scalaire du regard et de la direction tombe sous le cosinus du demi-cone");
        Check.False(targeting.IsEligible(Vector2.Zero, facing, candidates[3]), "trop loin : la portee se compare au CARRE, sans racine");
        Check.False(targeting.IsEligible(Vector2.Zero, facing, candidates[4]),
            "pile sur le cote : a 90 degres, on est exactement sur le bord du cone de 90, donc dehors");
        Check.False(targeting.IsEligible(Vector2.Zero, facing, Vector2.Zero),
            "et une cible exactement sur soi n'est pas ciblable : normaliser un vecteur nul rendrait NaN, et NaN passe tous les tests de comparaison en silence");

        Check.Equal(targeting.Pick(Vector2.Zero, facing, candidates), 1, "sans cible courante, on prend la plus proche du cone");

        var tie = new List<Vector2>
        {
            new Vector2(30f, 0f),
            new Vector2(30.5f, 0f),
        };

        var stable = new Targeting(range: 100f, coneDegrees: 120f, stickiness: 0.7f);

        Check.Equal(stable.Pick(Vector2.Zero, facing, tie), 0, "premiere image : la plus proche des deux");
        Check.Equal(stable.Pick(Vector2.Zero, facing, tie), 0, "deuxieme image : la meme");

        tie[1] = new Vector2(29.5f, 0f);

        Check.Equal(stable.Pick(Vector2.Zero, facing, tie), 0,
            "l'autre passe devant d'un demi-pixel, et la cible NE CHANGE PAS. C'est l'hysteresis : la cible courante voit son score multiplie par 0.7, donc il faut une avance NETTE pour la detroner");

        Check.True(stable.Stickiness < 1f, "sans elle, deux ennemis a distance egale se volent le reticule a chaque image, et il devient impossible de tirer");

        tie[1] = new Vector2(10f, 0f);

        Check.Equal(stable.Pick(Vector2.Zero, facing, tie), 1,
            "mais une cible franchement plus proche prend la main : coller n'est pas s'accrocher");

        var lost = new Targeting(range: 100f, coneDegrees: 90f, stickiness: 0.7f);
        var single = new List<Vector2> { new Vector2(30f, 0f) };

        Check.Equal(lost.Pick(Vector2.Zero, facing, single), 0, "une cible verrouillee");

        single[0] = new Vector2(300f, 0f);

        Check.Equal(lost.Pick(Vector2.Zero, facing, single), -1,
            "qui sort de la portee est LACHEE : l'hysteresis rend collant, elle ne doit jamais rendre aveugle");

        Check.Equal(lost.Current, -1, "et l'etat interne suit, sinon la prochaine image comparerait a un indice qui n'existe plus");

        Check.Equal(lost.Pick(Vector2.Zero, facing, new List<Vector2>()), -1, "une liste vide rend -1 sans planter");
    }
}
