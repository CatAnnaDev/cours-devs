namespace Csharplings;

public sealed class FixedClock
{
    private float _accumulator;

    public FixedClock(float step)
    {
        Step = step;
    }

    public float Step { get; }
    public int MaxStepsPerFrame { get; set; } = 5;

    public float Alpha => _accumulator / Step;

    public int Advance(float delta)
    {
        _accumulator += delta;

        int steps = 0;

        while (_accumulator >= Step && steps < MaxStepsPerFrame)
        {
            _accumulator -= Step;
            steps++;
        }

        if (steps == MaxStepsPerFrame)
            _accumulator = 0f;

        return steps;
    }
}

public static class Fixed1
{
    public const bool NotDone = false;

    public static void Run()
    {
        const float step = 1f / 60f;

        var clock = new FixedClock(step);

        Check.Equal(clock.Advance(step), 1, "une frame pile, un pas de physique");
        Check.Near(clock.Alpha, 0.0, "et il ne reste rien en attente");

        Check.Equal(clock.Advance(step / 2f), 0, "une demi-frame ne declenche rien");
        Check.Near(clock.Alpha, 0.5, "mais elle est mise de cote");
        Check.Equal(clock.Advance(step / 2f), 1, "la moitie suivante complete le pas");
        Check.Near(clock.Alpha, 0.0, "et le reste repart de zero");

        var slow = new FixedClock(step);

        Check.Equal(slow.Advance(step * 3f), 3, "une frame lente rattrape plusieurs pas d'un coup");

        var frozen = new FixedClock(step);

        Check.Equal(frozen.Advance(2f), 5, "un gel de deux secondes serait 120 pas : on plafonne");
        Check.Near(frozen.Alpha, 0.0, "et on jette le retard, sinon le jeu ne rattrape jamais son retard");

        var steady = new FixedClock(step);
        int total = 0;

        for (int frame = 0; frame < 60; frame++)
            total += steady.Advance(step);

        Check.Equal(total, 60, "soixante frames a 60 images par seconde : soixante pas");

        var laggy = new FixedClock(step);
        total = 0;

        for (int frame = 0; frame < 30; frame++)
            total += laggy.Advance(step * 2f);

        Check.Equal(total, 60, "trente frames a 30 images par seconde : exactement les memes soixante pas");
    }
}
