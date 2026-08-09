namespace Csharplings;

public sealed class Robot
{
    public Vector2 Position { get; set; }
    public int Energy { get; set; } = 10;
}

public interface ICommand
{
    void Do(Robot robot);
}

public sealed class Move : ICommand
{
    private readonly Vector2 _offset;

    public Move(Vector2 offset)
    {
        _offset = offset;
    }

    public void Do(Robot robot)
    {
        robot.Position += _offset;
        robot.Energy -= 1;
    }
}

public sealed class Recharge : ICommand
{
    public void Do(Robot robot)
    {
        robot.Energy += 5;
    }
}

public sealed class History
{
    private readonly Stack<ICommand> _done = new();
    private readonly Stack<ICommand> _undone = new();

    public int DoneCount => _done.Count;
    public int UndoneCount => _undone.Count;

    public void Execute(Robot robot, ICommand command)
    {
        command.Do(robot);
        _done.Push(command);
    }

    public bool Undo(Robot robot)
    {
        return Todo.Value<bool>();
    }

    public bool Redo(Robot robot)
    {
        return Todo.Value<bool>();
    }
}

public static class Command1
{
    public const bool NotDone = true;

    public static void Run()
    {
        var robot = new Robot();
        var history = new History();

        history.Execute(robot, new Move(new Vector2(10f, 0f)));
        history.Execute(robot, new Move(new Vector2(0f, 5f)));
        history.Execute(robot, new Recharge());

        Check.Near(robot.Position, new Vector2(10f, 5f), "trois ordres executes");
        Check.Equal(robot.Energy, 13, "dix, moins deux deplacements, plus cinq de recharge");
        Check.Equal(history.DoneCount, 3, "l'historique retient tout");

        Check.True(history.Undo(robot), "on annule la recharge");
        Check.Equal(robot.Energy, 8, "une commande sait defaire exactement ce qu'elle a fait");

        Check.True(history.Undo(robot), "on annule le deuxieme deplacement");
        Check.Near(robot.Position, new Vector2(10f, 0f), "on remonte l'historique pas a pas");
        Check.Equal(history.UndoneCount, 2, "les commandes annulees sont mises de cote");

        Check.True(history.Redo(robot), "et on peut refaire");
        Check.Near(robot.Position, new Vector2(10f, 5f), "en rejouant la commande telle quelle");

        history.Execute(robot, new Move(new Vector2(1f, 1f)));

        Check.Equal(history.UndoneCount, 0, "une nouvelle action efface la branche du futur");
        Check.False(history.Redo(robot), "il n'y a plus rien a refaire");

        var fresh = new History();

        Check.False(fresh.Undo(new Robot()), "annuler sans rien avoir fait ne doit pas planter");
    }
}
