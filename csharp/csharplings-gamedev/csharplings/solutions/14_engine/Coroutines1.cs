using System.Collections;

namespace Csharplings;

public sealed class WaitSeconds
{
    public WaitSeconds(float seconds)
    {
        Remaining = seconds;
    }

    public float Remaining { get; set; }
}

public sealed class CoroutineRunner
{
    private readonly List<IEnumerator> _routines = new();
    private readonly List<WaitSeconds> _waits = new();

    public int Count => _routines.Count;

    public void Start(IEnumerator routine)
    {
        _routines.Add(routine);
        _waits.Add(null);
    }

    public void Tick(float delta)
    {
        for (int i = _routines.Count - 1; i >= 0; i--)
        {
            WaitSeconds wait = _waits[i];

            if (wait is not null)
            {
                wait.Remaining -= delta;

                if (wait.Remaining > 0f)
                    continue;

                _waits[i] = null;
            }

            if (!_routines[i].MoveNext())
            {
                _routines.RemoveAt(i);
                _waits.RemoveAt(i);
                continue;
            }

            _waits[i] = _routines[i].Current as WaitSeconds;
        }
    }
}

public static class Coroutines1
{
    public const bool NotDone = false;

    public static IEnumerator Flash(List<string> log)
    {
        log.Add("debut");
        yield return new WaitSeconds(0.5f);

        log.Add("milieu");
        yield return null;

        log.Add("fin");
    }

    public static IEnumerator Beep(List<string> log)
    {
        log.Add("bip");
        yield return null;

        log.Add("bop");
    }

    public static void Run()
    {
        var flash = new List<string>();
        var beep = new List<string>();
        var runner = new CoroutineRunner();

        runner.Start(Flash(flash));
        runner.Start(Beep(beep));

        Check.Equal(flash.Count, 0, "demarrer une coroutine n'execute rien tout de suite");
        Check.Equal(runner.Count, 2, "mais elle est bien en attente");

        runner.Tick(0.25f);

        Check.Sequence(flash, new[] { "debut" }, "la premiere frame joue jusqu'au premier yield");
        Check.Sequence(beep, new[] { "bip" }, "les deux coroutines avancent en parallele");

        runner.Tick(0.25f);

        Check.Sequence(flash, new[] { "debut" }, "celle qui attend une demi-seconde ne bouge pas encore");
        Check.Sequence(beep, new[] { "bip", "bop" }, "l'autre a repris et s'est terminee");
        Check.Equal(runner.Count, 1, "une coroutine finie est retiree de la liste");

        runner.Tick(0.25f);

        Check.Sequence(flash, new[] { "debut", "milieu" }, "la demi-seconde est ecoulee, on reprend");

        runner.Tick(0.25f);

        Check.Sequence(flash, new[] { "debut", "milieu", "fin" }, "'yield return null' attend juste la frame suivante");
        Check.Equal(runner.Count, 0, "et la liste est vide");

        runner.Tick(1f);

        Check.Equal(runner.Count, 0, "animer un runner vide ne doit pas planter");
    }
}
