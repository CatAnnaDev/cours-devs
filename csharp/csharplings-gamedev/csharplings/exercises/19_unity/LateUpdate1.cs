using Csharplings.Unity;

namespace Csharplings;

public sealed class Walker : MonoBehaviour
{
    public float X { get; private set; }

    public override void Update() => X += 1f;
}

public sealed class CameraInUpdate : MonoBehaviour
{
    private readonly Walker _target;

    public CameraInUpdate(Walker target)
    {
        _target = target;
    }

    public float Seen { get; private set; }

    public override void Update() => Seen = _target.X;
}

public sealed class CameraInLateUpdate : MonoBehaviour
{
    private readonly Walker _target;

    public CameraInLateUpdate(Walker target)
    {
        _target = target;
    }

    public float Seen { get; private set; }

    public override void Update() => Seen = _target.X;
}

public sealed class PhysicsCounter : MonoBehaviour
{
    public int Steps { get; private set; }

    public override void FixedUpdate() => Steps++;
}

public sealed class InputProbe
{
    public bool JustPressed { get; set; }
}

public sealed class PollingInUpdate : MonoBehaviour
{
    private readonly InputProbe _input;

    public PollingInUpdate(InputProbe input)
    {
        _input = input;
    }

    public int Seen { get; private set; }

    public override void Update()
    {
        if (_input.JustPressed)
            Seen++;
    }
}

public sealed class PollingInFixedUpdate : MonoBehaviour
{
    private readonly InputProbe _input;

    public PollingInFixedUpdate(InputProbe input)
    {
        _input = input;
    }

    public int Seen { get; private set; }

    public override void Update()
    {
        if (_input.JustPressed)
            Seen++;
    }
}

public sealed class Tracer : MonoBehaviour
{
    public string Trace { get; private set; } = string.Empty;

    public void ClearTrace() => Trace = string.Empty;

    public override void Start() => Trace += "Start ";

    public override void FixedUpdate() => Trace += "Fixed ";

    public override void Update() => Trace += "Update ";

    public override void LateUpdate() => Trace += "Late ";
}

public sealed class Sleeper : MonoBehaviour
{
    public override void Update()
    {
    }
}

public static class LateUpdate1
{
    public const bool NotDone = true;

    public static void Run()
    {
        var scene = new Scene();
        var walker = new Walker();

        CameraInUpdate lagging = scene.Add(new CameraInUpdate(walker));
        CameraInLateUpdate correct = scene.Add(new CameraInLateUpdate(walker));

        scene.Add(walker);
        scene.Frames(5);

        Check.Near(walker.X, 5.0, "cinq frames, cinq pas");
        Check.Near(lagging.Seen, 4.0,
            "la camera qui suit dans Update a UNE FRAME DE RETARD : son Update est passe AVANT celui de sa cible, et l'ordre des scripts n'est pas garanti");
        Check.Near(correct.Seen, 5.0,
            "celle qui suit dans LateUpdate voit toujours la position finale : LateUpdate passe apres TOUS les Update, quel que soit l'ordre");
        Check.Near(walker.X - lagging.Seen, 1.0,
            "le retard vaut exactement un deplacement de frame. C'est ca, la camera qui tremble");

        var traced = new Scene();
        Tracer tracer = traced.Add(new Tracer());

        traced.Frame();

        Check.Equal(tracer.Trace, "Start Update Late ",
            "premiere frame : Start passe avant le premier Update, et LateUpdate ferme la marche. Aucun pas de physique encore");

        tracer.ClearTrace();
        traced.Frame();

        Check.Equal(tracer.Trace, "Fixed Update Late ",
            "et le pas de physique passe AVANT l'Update de sa frame, jamais apres");

        var physics = new Scene();
        PhysicsCounter counter = physics.Add(new PhysicsCounter());

        physics.Frame();

        Check.Equal(physics.FixedStepsLastFrame, 0,
            "une frame de 1/60 ne remplit pas un pas de 1/50 : ZERO FixedUpdate cette frame");
        Check.Equal(counter.Steps, 0, "le compteur n'a pas bouge");

        physics.Frame();

        Check.Equal(physics.FixedStepsLastFrame, 1, "la frame suivante en declenche un");

        physics.Frame(delta: 0.5);

        Check.Equal(physics.FixedStepsLastFrame, 17,
            "une frame qui a dure une demi-seconde en declencherait vingt-cinq. Il n'y en a que dix-sept : Unity PLAFONNE le delta a un tiers de seconde, sinon un gel en genererait des centaines et le jeu ne rattraperait jamais son retard");

        var second = new Scene();
        PhysicsCounter steady = second.Add(new PhysicsCounter());

        second.Frames(61);

        Check.Equal(steady.Steps, 50,
            "soixante et une frames de rendu pour cinquante pas de physique : les deux boucles ne sont pas cadencees pareil, et c'est voulu");

        var pressed = new Scene();
        var probe = new InputProbe();

        PollingInUpdate inUpdate = pressed.Add(new PollingInUpdate(probe));
        PollingInFixedUpdate inFixed = pressed.Add(new PollingInFixedUpdate(probe));

        probe.JustPressed = true;
        pressed.Frame();
        probe.JustPressed = false;

        Check.Equal(pressed.FixedStepsLastFrame, 0, "cette frame-la n'a joue aucun pas de physique");
        Check.Equal(inUpdate.Seen, 1, "l'appui lu dans Update est vu une fois, exactement");
        Check.Equal(inFixed.Seen, 0,
            "celui lu dans FixedUpdate est PERDU : il n'y a pas eu de pas cette frame. C'est comme ca qu'un saut ne part pas, une fois sur six");

        var repeated = new Scene();
        var held = new InputProbe { JustPressed = true };
        PollingInFixedUpdate twice = repeated.Add(new PollingInFixedUpdate(held));

        repeated.Frame(delta: 0.05);

        Check.Equal(repeated.FixedStepsLastFrame, 2, "une frame de 0.05 s joue deux pas");
        Check.Equal(twice.Seen, 2,
            "et le MEME appui est lu deux fois : le joueur saute deux fois pour une seule pression. La parade : lire dans Update, memoriser, consommer dans FixedUpdate");

        var quiet = new Scene();

        quiet.Add(new Sleeper());
        quiet.Add(new Sleeper());

        MonoBehaviour.EngineCallbacks = 0;
        quiet.Frames(10);

        Check.Equal(MonoBehaviour.EngineCallbacks, 0,
            "deux scripts qui ne declarent AUCUNE fonction de boucle : zero appel du moteur. Unity ne branche que ce que tu ecris, un Update absent ne coute rien");

        MonoBehaviour.EngineCallbacks = 0;

        var busy = new Scene();

        busy.Add(new Tracer());
        busy.Frames(61);

        Check.Equal(MonoBehaviour.EngineCallbacks, 1 + 50 + 61 + 61,
            "un script qui declare les trois : un Start, cinquante FixedUpdate, soixante et un Update, soixante et un LateUpdate. Chacun est un franchissement");
    }
}
