namespace Csharplings;

public sealed class GameClock
{
    public float Scale { get; set; } = 1f;

    public float Delta { get; private set; }

    public float UnscaledDelta { get; private set; }

    public double Elapsed { get; private set; }

    public double UnscaledElapsed { get; private set; }

    public bool IsPaused => Mathf.IsZeroApprox(Scale);

    public float FramesPerSecond => 1f / Delta;

    public void Advance(float realDelta)
    {
        UnscaledDelta = realDelta;
        Delta = realDelta;
        UnscaledElapsed += realDelta;
        Elapsed += Delta;
    }
}

public sealed class Countdown
{
    private float _remaining;

    public Countdown(float duration, bool ignoresPause)
    {
        Duration = duration;
        IgnoresPause = ignoresPause;
        _remaining = duration;
    }

    public float Duration { get; }

    public bool IgnoresPause { get; }

    public float Remaining => _remaining;

    public bool IsDone => _remaining <= 0f;

    public void Tick(GameClock clock)
    {
        float delta = clock.Delta;

        _remaining = Mathf.Max(_remaining - delta, 0f);
    }
}

public static class Scale1
{
    public const bool NotDone = true;

    private const float Frame = 1f / 60f;

    public static void Run()
    {
        var clock = new GameClock();

        var gameplay = new Countdown(1f, ignoresPause: false);
        var menu = new Countdown(1f, ignoresPause: true);

        clock.Advance(Frame);
        gameplay.Tick(clock);
        menu.Tick(clock);

        Check.Near(clock.Delta, Frame, "a l'echelle 1, le temps de jeu est le temps reel");
        Check.Near(clock.UnscaledDelta, Frame, "et le temps reel est le temps reel");
        Check.False(clock.IsPaused, "on n'est pas en pause");
        Check.Near(clock.FramesPerSecond, 60.0, "soixante images par seconde", 0.01);

        clock.Scale = 0.5f;
        clock.Advance(Frame);
        gameplay.Tick(clock);
        menu.Tick(clock);

        Check.Near(clock.Delta, Frame * 0.5f, "au ralenti, le jeu recoit la moitie du temps");
        Check.Near(clock.UnscaledDelta, Frame, "mais la machine tourne toujours a la meme vitesse");
        Check.Near(clock.FramesPerSecond, 60.0,
            "le compteur d'images se calcule sur le temps REEL : sinon un ralenti afficherait 30 fps alors que rien n'a ralenti", 0.01);

        clock.Scale = 0f;

        for (int frame = 0; frame < 60; frame++)
        {
            clock.Advance(Frame);
            gameplay.Tick(clock);
            menu.Tick(clock);
        }

        Check.True(clock.IsPaused, "echelle zero, le jeu est en pause");
        Check.Near(clock.Delta, 0.0, "le temps de jeu ne s'ecoule plus du tout");
        Check.Near(clock.UnscaledDelta, Frame, "le temps reel, lui, continue : c'est lui qui fait vivre le menu");
        Check.False(float.IsInfinity(clock.FramesPerSecond),
            "et surtout : 1 / Delta pendant une pause donne l'infini. Le compteur doit diviser le temps REEL");
        Check.Near(clock.FramesPerSecond, 60.0, "il affiche donc toujours 60", 0.01);

        Check.Near(gameplay.Remaining, 1.0 - Frame * 1.5,
            "une seconde de pause n'a rien enleve au chrono de jeu : il en est reste ou il etait avant la pause", 0.001);
        Check.True(menu.IsDone,
            "alors que le chrono du menu, lui, est arrive au bout : animations d'interface, fondus de son et matchmaking doivent ignorer la pause");

        clock.Scale = 2f;
        clock.Advance(Frame);

        Check.Near(clock.Delta, Frame * 2f, "en acceleration, le jeu recoit deux fois plus de temps");

        Check.True(clock.UnscaledElapsed > clock.Elapsed,
            "le temps reel total a depasse le temps de jeu total : tout ce qui a ete mis en pause manque a l'appel");
        Check.Near(clock.UnscaledElapsed, 63.0 * Frame, "63 frames se sont vraiment ecoulees", 0.001);
        Check.Near(clock.Elapsed, 3.5 * Frame,
            "mais le jeu n'en a vu que trois et demie : 1 plus 0.5 plus 0 fois 60 plus 2", 0.001);
    }
}
