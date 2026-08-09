namespace Csharplings;

public sealed class HealthPool
{
    private static readonly StringName Changed = new("health_changed");

    private readonly SignalBus _bus = new();

    public event Action<int> ChangedInCSharp;

    public int Current { get; private set; } = 100;

    public void ConnectSignal(Action<Variant[]> handler) => _bus.Connect(Changed, handler);

    public void HurtThroughSignal(int amount)
    {
        Current -= amount;
        _bus.Emit(Changed, Current, amount);
    }

    public void HurtThroughEvent(int amount)
    {
        Current -= amount;
        ChangedInCSharp?.Invoke(Current);
    }

    public void HurtSilently(int amount)
    {
        Current -= amount;
        _bus.Emit(Changed);
    }
}

public static class Signals1
{
    public const bool NotDone = false;

    public static void Run()
    {
        var pool = new HealthPool();
        int fromSignal = 0;
        int fromEvent = 0;

        pool.ConnectSignal(arguments =>
        {
            if (arguments.Length > 0)
                fromSignal = arguments[0].AsInt();
        });
        pool.ChangedInCSharp += value => fromEvent = value;

        pool.HurtThroughSignal(10);

        Check.Equal(pool.Current, 90, "le signal a bien transmis l'etat");
        Check.Equal(fromSignal, 90, "et l'abonne l'a recu");

        pool.HurtThroughEvent(10);

        Check.Equal(fromEvent, 80, "l'event C# aussi, avec un typage direct au lieu d'un tableau a indexer");

        long withArguments = Report("une emission de signal a deux arguments", Allocations(() => pool.HurtThroughSignal(0)));
        long withoutArguments = Report("une emission de signal sans argument", Allocations(() => pool.HurtSilently(0)));
        long throughEvent = Report("un event C# invoque", Allocations(() => pool.HurtThroughEvent(0)));

        Check.True(withArguments > 0L,
            "un signal a arguments alloue : les arguments partent dans un tableau, et 'params' en fabrique un a CHAQUE appel");
        Check.Equal(withoutArguments, 0L,
            "sans argument, non : le compilateur passe un tableau vide partage. Un signal qui ne porte rien est gratuit");
        Check.Equal(throughEvent, 0L,
            "et un event C# type ne coute rien du tout, quel que soit le nombre d'arguments");

        Check.True(withArguments > throughEvent,
            "d'ou la regle : signal moteur pour ce que l'editeur doit voir et brancher, event C# pour ce qui reste entre scripts");

        Check.True(SignalBus.EmitCalls > 0, "les emissions ont bien eu lieu");
        Check.Equal(fromSignal, pool.Current,
            "et remarque au passage : un abonne a un signal recoit un tableau non type, qu'il doit indexer et convertir a la main en verifiant sa taille. L'event C# lui donnait un int");

        var busy = new HealthPool();
        int calls = 0;

        busy.ConnectSignal(_ => calls++);
        SignalBus.ResetCounter();

        for (int frame = 0; frame < 60; frame++)
            busy.HurtThroughSignal(0);

        Check.Equal(calls, 60, "soixante emissions en une seconde de jeu");
        Check.Equal(SignalBus.EmitCalls, 60, "soixante franchissements, et soixante tableaux d'arguments");

        long perSecond = Report("le cout d'une seconde de ce signal", Allocations(() => Sixty(busy)));

        Check.True(perSecond >= withArguments * 60L,
            "multiplie par le nombre de frames, une emission par frame devient un dechet permanent : c'est ce qui remplit la generation zero");
    }

    private static void Sixty(HealthPool pool)
    {
        for (int frame = 0; frame < 60; frame++)
            pool.HurtThroughSignal(0);
    }

    private static long Allocations(Action action)
    {
        action();
        action();
        action();

        long before = GC.GetAllocatedBytesForCurrentThread();
        action();

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static long Report(string what, long bytes)
    {
        Console.WriteLine($"      mesure  {what} : {bytes} octets");

        return bytes;
    }
}
