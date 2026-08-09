namespace Csharplings;

public enum PlayerState
{
    Idle,
    Run,
    Jump,
    Fall,
    Dead,
}

public sealed class StateMachine
{
    private readonly List<string> _log = new();

    public PlayerState Current { get; private set; } = PlayerState.Idle;

    public IReadOnlyList<string> Log => _log;

    public bool CanGoTo(PlayerState next) => (Current, next) switch
    {
        (PlayerState.Dead, _) => false,
        (_, PlayerState.Dead) => true,
        (PlayerState.Idle, PlayerState.Run) => true,
        (PlayerState.Idle, PlayerState.Jump) => true,
        (PlayerState.Run, PlayerState.Idle) => true,
        (PlayerState.Run, PlayerState.Jump) => true,
        (PlayerState.Jump, PlayerState.Fall) => true,
        (PlayerState.Fall, PlayerState.Idle) => true,
        _ => false,
    };

    public bool TryGoTo(PlayerState next)
    {
        if (next == Current || !CanGoTo(next))
            return false;

        _log.Add($"sort {Current}");
        Current = next;
        _log.Add($"entre {Current}");

        return true;
    }
}

public static class States1
{
    public const bool NotDone = false;

    public static void Run()
    {
        var machine = new StateMachine();

        Check.Equal(machine.Current, PlayerState.Idle, "on demarre a l'arret");

        Check.True(machine.TryGoTo(PlayerState.Run), "de l'arret on peut courir");
        Check.True(machine.TryGoTo(PlayerState.Jump), "en courant on peut sauter");
        Check.False(machine.TryGoTo(PlayerState.Run), "mais en l'air on ne repart pas en course");
        Check.Equal(machine.Current, PlayerState.Jump, "une transition refusee ne change rien");

        Check.False(machine.TryGoTo(PlayerState.Jump), "et re-entrer dans l'etat courant ne compte pas");

        Check.Sequence(machine.Log,
            new[] { "sort Idle", "entre Run", "sort Run", "entre Jump" },
            "chaque transition acceptee declenche une sortie puis une entree");

        Check.True(machine.TryGoTo(PlayerState.Fall), "d'un saut on retombe");
        Check.True(machine.TryGoTo(PlayerState.Idle), "et on atterrit");

        Check.True(machine.TryGoTo(PlayerState.Dead), "on peut mourir depuis n'importe quel etat");
        Check.False(machine.TryGoTo(PlayerState.Idle), "mais on ne revient pas de la mort");
        Check.False(machine.TryGoTo(PlayerState.Run), "quel que soit l'etat vise");
        Check.Equal(machine.Current, PlayerState.Dead, "l'etat final est un puits");
    }
}
