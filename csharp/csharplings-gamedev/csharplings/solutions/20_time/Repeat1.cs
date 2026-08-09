namespace Csharplings;

public sealed class ResetToZeroTimer
{
    private float _elapsed;

    public ResetToZeroTimer(float interval)
    {
        Interval = interval;
    }

    public float Interval { get; }

    public int Advance(float delta)
    {
        _elapsed += delta;

        if (_elapsed < Interval)
            return 0;

        _elapsed = 0f;

        return 1;
    }
}

public sealed class SubtractIntervalTimer
{
    private float _elapsed;

    public SubtractIntervalTimer(float interval)
    {
        Interval = interval;
    }

    public float Interval { get; }

    public int Advance(float delta)
    {
        _elapsed += delta;

        int fires = 0;

        while (_elapsed >= Interval)
        {
            _elapsed -= Interval;
            fires++;
        }

        return fires;
    }
}

public sealed class ScheduledTimer
{
    public ScheduledTimer(double interval)
    {
        Interval = interval;
    }

    public double Interval { get; }

    public long FireCount { get; private set; }

    public double NextFireAt => (FireCount + 1) * Interval;

    public int AdvanceTo(double absoluteTime)
    {
        int fires = 0;

        while (NextFireAt <= absoluteTime)
        {
            FireCount++;
            fires++;
        }

        return fires;
    }
}

public static class Repeat1
{
    public const bool NotDone = false;

    private const float Interval = 0.1f;
    private const double ScheduleInterval = 0.1;
    private const float Delta = 0.03f;
    private const int Calls = 333;

    public static void Run()
    {
        var subtract = new SubtractIntervalTimer(Interval);

        Check.Equal(subtract.Advance(0.05f), 0, "la moitie de l'intervalle ne declenche rien");
        Check.Equal(subtract.Advance(0.05f), 1, "l'autre moitie le complete");
        Check.Equal(subtract.Advance(0.25f), 2,
            "un delta de 0.25 avec un intervalle de 0.1 doit declencher DEUX fois, pas une : sinon une frame lente avale des evenements");
        Check.Equal(subtract.Advance(0.06f), 1,
            "et les 0.05 mis de cote comptent pour la suite : 0.06 de plus suffit a repasser la barre");

        int drifting = Total(new ResetToZeroTimer(Interval));
        int exact = Total(new SubtractIntervalTimer(Interval));

        Report("remise a zero apres chaque declenchement", drifting);
        Report("soustraction de l'intervalle", exact);

        Check.Equal(exact, 99, "333 appels de 0.03 s font 9.99 s : a 0.1 s d'intervalle, 99 declenchements");
        Check.Equal(drifting, 83,
            "remettre le compteur a ZERO jette le reste a chaque fois : 16 declenchements perdus sur 10 secondes, et l'ecart grandit sans arret");
        Check.True(exact > drifting, "c'est la DERIVE, et c'est le bug de la moitie des timers ecrits a la main");

        var schedule = new ScheduledTimer(ScheduleInterval);
        double now = 0.0;

        for (int call = 0; call < Calls; call++)
        {
            now += Delta;
            schedule.AdvanceTo(now);
        }

        Check.Equal(schedule.FireCount, 99L, "la troisieme approche compte pareil");
        Check.Near(schedule.NextFireAt, 10.0,
            "mais elle sait EXACTEMENT quand repartir, parce qu'elle ne s'accumule pas : elle MULTIPLIE le compte par l'intervalle", 1e-9);

        var recovering = new ScheduledTimer(ScheduleInterval);

        Check.Equal(recovering.AdvanceTo(1.0), 10, "un gel d'une seconde rattrape ses dix declenchements");
        Check.Near(recovering.NextFireAt, 1.1, "et la suite reste alignee sur la grille de depart", 1e-9);

        var farFuture = new ScheduledTimer(ScheduleInterval);

        Check.Equal(farFuture.AdvanceTo(60.05), 600,
            "attention quand meme : un rattrapage non plafonne peut declencher 600 fois d'un coup et noyer la frame");
    }

    private static int Total(ResetToZeroTimer timer)
    {
        int fires = 0;

        for (int call = 0; call < Calls; call++)
            fires += timer.Advance(Delta);

        return fires;
    }

    private static int Total(SubtractIntervalTimer timer)
    {
        int fires = 0;

        for (int call = 0; call < Calls; call++)
            fires += timer.Advance(Delta);

        return fires;
    }

    private static void Report(string what, int fires) =>
        Console.WriteLine($"      mesure  {what} : {fires} declenchements");
}
