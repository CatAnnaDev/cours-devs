namespace Csharplings;

public static class Intercept1
{
    public const bool NotDone = false;

    public static bool TryLead(Vector2 shooter, Vector2 target, Vector2 targetVelocity, float projectileSpeed, out float time)
    {
        time = 0f;

        Vector2 offset = target - shooter;
        float a = targetVelocity.LengthSquared() - projectileSpeed * projectileSpeed;
        float b = 2f * offset.Dot(targetVelocity);
        float c = offset.LengthSquared();

        if (Mathf.IsZeroApprox(a))
        {
            if (Mathf.IsZeroApprox(b))
                return false;

            time = -c / b;

            return time >= 0f;
        }

        float discriminant = b * b - 4f * a * c;

        if (discriminant < 0f)
            return false;

        float root = Mathf.Sqrt(discriminant);
        float first = (-b - root) / (2f * a);
        float second = (-b + root) / (2f * a);

        if (first > second)
            (first, second) = (second, first);

        time = first >= 0f ? first : second;

        return time >= 0f;
    }

    public static bool TryAim(Vector2 shooter, Vector2 target, Vector2 targetVelocity, float projectileSpeed, out Vector2 aimPoint)
    {
        aimPoint = target;

        if (!TryLead(shooter, target, targetVelocity, projectileSpeed, out float time))
            return false;

        aimPoint = target + targetVelocity * time;

        return true;
    }

    public static void Run()
    {
        Check.True(TryLead(Vector2.Zero, new Vector2(100f, 0f), Vector2.Zero, 50f, out float still),
            "une cible immobile se touche toujours");
        Check.Near(still, 2f, "et le temps de vol est la simple distance divisee par la vitesse");

        Check.True(TryAim(Vector2.Zero, new Vector2(100f, 0f), Vector2.Zero, 50f, out Vector2 stillPoint),
            "on vise donc la cible elle-meme");
        Check.Near(stillPoint, new Vector2(100f, 0f), "sans avance");

        Check.True(TryLead(Vector2.Zero, new Vector2(0f, 100f), new Vector2(30f, 0f), 50f, out float crossing),
            "une cible qui traverse devant soi se touche aussi");

        Check.True(TryAim(Vector2.Zero, new Vector2(0f, 100f), new Vector2(30f, 0f), 50f, out Vector2 lead),
            "en visant DEVANT elle");
        Check.True(lead.X > 0f, "du cote ou elle va, jamais la ou elle est");
        Check.Near(lead, new Vector2(30f * crossing, 100f), "exactement la ou elle sera dans le temps de vol");

        Check.Near((lead - Vector2.Zero).Length(), 50f * crossing,
            "et la verification qui prouve que le calcul est juste : la distance a parcourir vaut la vitesse du projectile fois le temps de vol");

        Check.False(TryLead(Vector2.Zero, new Vector2(100f, 0f), new Vector2(80f, 0f), 50f, out _),
            "une cible qui FUIT plus vite que le projectile est intouchable, et il faut le dire : le discriminant est negatif, il n'y a pas de solution reelle");

        Check.True(TryLead(Vector2.Zero, new Vector2(100f, 0f), new Vector2(-80f, 0f), 50f, out float closing),
            "la meme cible qui FONCE sur soi se touche, elle, meme si elle est plus rapide");
        Check.True(closing > 0f, "et le temps de vol reste positif : un temps NEGATIF serait une solution dans le passe, et il faut prendre l'autre racine");

        Check.False(TryLead(Vector2.Zero, new Vector2(100f, 0f), new Vector2(50f, 0f), 50f, out _),
            "cas special : la cible S'ELOIGNE exactement a la vitesse du projectile. Le terme carre s'annule, l'equation devient LINEAIRE, et la reponse est non - l'ecart ne se comble jamais. Sans ce test on divise par zero et le tir part vers NaN");

        Check.True(TryLead(Vector2.Zero, new Vector2(-100f, 0f), new Vector2(50f, 0f), 50f, out float linear),
            "la meme vitesse mais en APPROCHE : l'equation reste lineaire, et cette fois la solution existe");

        Check.Near(linear, 1f, "ils se rejoignent a mi-chemin, une seconde plus tard : cinquante unites chacun");

        Check.True(TryAim(new Vector2(10f, 10f), new Vector2(10f, 10f), new Vector2(5f, 0f), 50f, out Vector2 onTop),
            "une cible collee au canon se touche immediatement");
        Check.Near(onTop, new Vector2(10f, 10f), "au point ou elle est, parce que le temps de vol est nul", 0.01);
    }
}
