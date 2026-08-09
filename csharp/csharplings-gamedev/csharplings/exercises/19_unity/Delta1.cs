using Csharplings.Unity;

namespace Csharplings;

public sealed class WithoutDelta : MonoBehaviour
{
    public float X { get; private set; }

    public override void Update() => X += 2f;
}

public sealed class WithDelta : MonoBehaviour
{
    public float X { get; private set; }

    public override void Update() => X += 120f;
}

public sealed class DeltaProbe : MonoBehaviour
{
    public float SeenInUpdate { get; private set; }

    public float SeenInFixedUpdate { get; private set; }

    public float UnscaledSeenInUpdate { get; private set; }

    public override void Update()
    {
        SeenInUpdate = Time.DeltaTime;
        UnscaledSeenInUpdate = Time.DeltaTime;
    }

    public override void FixedUpdate() => SeenInFixedUpdate = Time.DeltaTime;
}

public static class Delta1
{
    public const bool NotDone = true;

    public static void Run()
    {
        Time.Reset();

        var sixty = new Scene();
        WithoutDelta naiveSlow = sixty.Add(new WithoutDelta());
        WithDelta properSlow = sixty.Add(new WithDelta());

        sixty.Frames(60, 1.0 / 60.0);

        Time.Reset();

        var hundredTwenty = new Scene();
        WithoutDelta naiveFast = hundredTwenty.Add(new WithoutDelta());
        WithDelta properFast = hundredTwenty.Add(new WithDelta());

        hundredTwenty.Frames(120, 1.0 / 120.0);

        Check.Near(naiveSlow.X, 120.0, "une seconde a 60 images par seconde, deux unites par frame : 120");
        Check.Near(naiveFast.X, 240.0,
            "la MEME seconde a 120 images par seconde : 240. Sans delta, ton jeu va deux fois plus vite sur un meilleur ecran");

        Check.Near(properSlow.X, 120.0, "avec Time.deltaTime, une seconde vaut 120 unites", 0.01);
        Check.Near(properFast.X, properSlow.X,
            "et c'est la meme distance aux deux framerates : c'est tout ce qu'on demande a delta", 0.01);

        Time.Reset();

        var probeScene = new Scene();
        DeltaProbe probe = probeScene.Add(new DeltaProbe());

        probeScene.Frames(10, 1.0 / 60.0);

        Check.Near(probe.SeenInUpdate, 1.0 / 60.0, "dans Update, Time.deltaTime est bien le temps de la frame", 0.0001);
        Check.Near(probe.SeenInFixedUpdate, 0.02,
            "mais dans FixedUpdate, le MEME Time.deltaTime rend le pas fixe et non le temps de la frame. Piege classique : le code a l'air correct des deux cotes", 0.0001);
        Check.True(probe.SeenInFixedUpdate > probe.SeenInUpdate,
            "ici le pas fixe est meme plus GRAND que la frame : 0.02 contre 0.0167");

        Time.Reset();

        var hitch = new Scene();
        DeltaProbe hitched = hitch.Add(new DeltaProbe());

        hitch.Frame(delta: 2.0);

        Check.Near(hitched.SeenInUpdate, 1.0 / 3.0,
            "un gel de deux secondes n'arrive PAS tel quel dans ton code : Time.deltaTime est plafonne a maximumDeltaTime, un tiers de seconde par defaut", 0.001);
        Check.Near(Time.RealtimeSinceStartup, 2.0,
            "le temps reel, lui, sait tres bien que deux secondes se sont ecoulees", 0.001);
        Check.True(Time.TimeSinceStart < 0.5f,
            "mais le temps de jeu n'en a compte qu'un tiers. C'est ce plafond qui evite qu'un projectile traverse un mur apres un chargement");

        Time.Reset();
        Time.TimeScale = 0.5f;

        var slowmo = new Scene();
        DeltaProbe slowed = slowmo.Add(new DeltaProbe());

        slowmo.Frame(delta: 1.0 / 60.0);

        Check.Near(slowed.SeenInUpdate, 1.0 / 120.0, "au ralenti, Time.deltaTime est divise par deux", 0.0001);
        Check.Near(slowed.UnscaledSeenInUpdate, 1.0 / 60.0,
            "alors que unscaledDeltaTime rend le temps reel de la frame : c'est celui d'un menu de pause, d'un fondu de son ou d'une animation d'interface", 0.0001);

        Time.Reset();

        var clocks = new Scene();

        Time.TimeScale = 0f;
        clocks.Frames(60, 1.0 / 60.0);

        Time.TimeScale = 1f;
        clocks.Frames(60, 1.0 / 60.0);

        Check.Near(Time.TimeSinceStart, 1.0,
            "une seconde de pause puis une seconde de jeu : le temps de JEU n'a compte que la seconde jouee", 0.01);
        Check.Near(Time.UnscaledTimeSinceStart, 2.0, "le temps non mis a l'echelle a compte les deux", 0.01);
        Check.Near(Time.RealtimeSinceStartup, 2.0, "et le temps reel aussi", 0.01);
        Check.Equal(Time.FrameCount, 120, "pour 120 frames affichees");

        Check.True(Time.FrameCount / 60 != (int)Time.TimeSinceStart,
            "d'ou la derniere regle : compter en frames n'est pas compter en secondes. 120 frames ici, mais une seule seconde de jeu");
    }
}
