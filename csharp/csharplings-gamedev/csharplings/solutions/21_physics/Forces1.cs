namespace Csharplings;

public static class Forces1
{
    public const bool NotDone = false;

    private const float Stiffness = 400f;
    private const float Mass = 2f;

    public static float ExplicitSpringPeak(float delta, int steps)
    {
        float position = 1f;
        float velocity = 0f;
        float peak = 0f;

        for (int step = 0; step < steps; step++)
        {
            float acceleration = -Stiffness * position;

            position += velocity * delta;
            velocity += acceleration * delta;

            peak = Mathf.Max(peak, Mathf.Abs(position));
        }

        return peak;
    }

    public static float SemiImplicitSpringPeak(float delta, int steps)
    {
        float position = 1f;
        float velocity = 0f;
        float peak = 0f;

        for (int step = 0; step < steps; step++)
        {
            velocity += -Stiffness * position * delta;
            position += velocity * delta;

            peak = Mathf.Max(peak, Mathf.Abs(position));
        }

        return peak;
    }

    public static float ApplyImpulse(float velocity, float impulse) => velocity + impulse / Mass;

    public static float ApplyForce(float velocity, float force, float delta) => velocity + force / Mass * delta;

    public static float NaiveDamping(float velocity, float perFrame) => velocity * perFrame;

    public static float StableDamping(float velocity, float perSecond, float delta) =>
        velocity * Mathf.Pow(perSecond, delta);

    public static void Run()
    {
        const float step = 1f / 60f;
        const int oneSecond = 60;

        float explicitPeak = ExplicitSpringPeak(step, 600);
        float semiImplicitPeak = SemiImplicitSpringPeak(step, 600);

        Report("ressort en Euler explicite, amplitude maximale sur 10 s", explicitPeak);
        Report("le meme en Euler semi-implicite", semiImplicitPeak);

        Check.Near(semiImplicitPeak, 1.0,
            "mettre a jour la VITESSE d'abord, puis la position avec la nouvelle vitesse : l'amplitude reste celle du depart", 0.05);
        Check.True(explicitPeak > 1000f * semiImplicitPeak,
            "l'ordre inverse ajoute de l'energie a chaque pas : le ressort EXPLOSE, et c'est pour ca qu'aucun moteur n'integre comme ca");
        Check.True(float.IsFinite(explicitPeak), "il ne part pas encore a l'infini sur dix secondes, mais l'ecart est deja astronomique");

        Check.Near(ApplyImpulse(0f, 10f), 5.0,
            "une impulsion change la vitesse d'un coup : dix newton-secondes sur deux kilos, c'est cinq metres par seconde");
        Check.Near(ApplyImpulse(ApplyImpulse(0f, 10f), 10f), 10.0,
            "deux impulsions s'additionnent, et aucune des deux ne regarde delta : c'est ce qui les rend utilisables pour un saut ou un tir");

        float pushed = 0f;

        for (int frame = 0; frame < oneSecond; frame++)
            pushed = ApplyForce(pushed, 10f, step);

        Check.Near(pushed, 5.0,
            "une force continue de dix newtons pendant une seconde arrive au meme resultat, mais etale sur soixante frames", 0.001);

        float finerStep = 1f / 240f;
        float fine = 0f;

        for (int frame = 0; frame < oneSecond * 4; frame++)
            fine = ApplyForce(fine, 10f, finerStep);

        Check.Near(fine, pushed, "et quel que soit le pas de temps, puisque la force est multipliee par delta", 0.001);

        float naiveSlow = 100f;
        float naiveFast = 100f;

        for (int frame = 0; frame < oneSecond; frame++)
            naiveSlow = NaiveDamping(naiveSlow, 0.98f);

        for (int frame = 0; frame < oneSecond * 4; frame++)
            naiveFast = NaiveDamping(naiveFast, 0.98f);

        Report("amortissement en 0.98 par frame, apres 1 s a 60 images", naiveSlow);
        Report("le meme apres 1 s a 240 images", naiveFast);

        Check.Near(naiveSlow, 29.76, "ton 0.98 par frame etait regle pour soixante images par seconde", 0.05);
        Check.True(naiveFast < 1f,
            "a 240 images il ne reste plus RIEN : multiplier par une constante a chaque frame, c'est regler son jeu pour un seul ecran");

        float stableSlow = 100f;
        float stableFast = 100f;

        for (int frame = 0; frame < oneSecond; frame++)
            stableSlow = StableDamping(stableSlow, 0.3f, step);

        for (int frame = 0; frame < oneSecond * 4; frame++)
            stableFast = StableDamping(stableFast, 0.3f, finerStep);

        Report("amortissement en Pow(0.3, delta), apres 1 s a 60 images", stableSlow);
        Report("le meme apres 1 s a 240 images", stableFast);

        Check.Near(stableSlow, 30.0, "garder 30 pour cent par SECONDE se lit directement dans le reglage", 0.05);
        Check.Near(stableFast, stableSlow, "et donne le meme resultat a n'importe quel framerate", 0.05);
    }

    private static void Report(string what, float value) =>
        Console.WriteLine($"      mesure  {what} : {value:0.####}");
}
