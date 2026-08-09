namespace Csharplings;

public static class Names1
{
    public const bool NotDone = true;

    private static readonly StringName Died = new("died");

    private static readonly StringName HealthChanged = new("HealthChanged");

    public static void Emit100Times(SignalBus bus, StringName signal)
    {
        for (int i = 0; i < 100; i++)
            bus.Emit(signal.Value);
    }

    public static void Emit100TimesFromText(SignalBus bus)
    {
        for (int i = 0; i < 100; i++)
            bus.Emit("died");
    }

    public static void Run()
    {
        var bus = new SignalBus();
        int received = 0;

        bus.Connect(Died, _ => received++);

        Check.Equal(Died.Value, "died", "un nom moteur enveloppe une chaine");
        Check.True(ReferenceEquals(Died, Died), "et celui-la est fabrique une seule fois, au chargement de la classe");

        StringName.ResetCounter();
        Emit100TimesFromText(bus);

        int fromText = Report("100 emissions ecrites avec une chaine litterale", StringName.Created);

        Check.Equal(received, 100, "les cent emissions sont bien arrivees");
        Check.Equal(fromText, 100,
            "mais chaque appel a converti la chaine en nom moteur : CENT objets pour cent emissions. La conversion est implicite, donc invisible a la lecture");

        StringName.ResetCounter();
        Emit100Times(bus, Died);

        int fromCache = Report("les memes avec un nom garde en champ static readonly", StringName.Created);

        Check.Equal(received, 200, "meme resultat");
        Check.Equal(fromCache, 0,
            "et ZERO objet cree. C'est exactement a quoi servent les SignalName.X et PropertyName.X que Godot genere pour toi");

        Check.True(fromText > fromCache, "cent contre zero, pour un comportement identique");

        Check.True(Report("cout en octets d'une emission depuis une chaine", Allocations(() => bus.Emit("died"))) > 0L,
            "en octets, la conversion se voit aussi");

        long cached = Report("cout en octets d'une emission avec le nom garde", Allocations(() => bus.Emit(Died)));

        Check.Equal(cached, 0L,
            "alors que la version avec nom garde ne coute rien du tout : pas de conversion, et pas d'arguments a emballer");

        StringName.ResetCounter();

        var withArgument = new SignalBus();
        int total = 0;

        withArgument.Connect(HealthChanged, arguments => total += arguments[0].AsInt());
        withArgument.Emit(HealthChanged, 25);

        Check.Equal(total, 25, "un signal peut porter des valeurs");
        Check.Equal(StringName.Created, 0, "et le nom garde ne coute toujours rien");

        Check.Equal(HealthChanged.Value, "HealthChanged",
            "et un detail verifie dans le moteur : un signal DECLARE en C# garde le nom PascalCase de son delegate. Ce sont les signaux INTEGRES de Godot qui sont en minuscules avec des underscores, comme body_entered ou tree_exiting");
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

    private static int Report(string what, int created)
    {
        Console.WriteLine($"      mesure  {what} : {created} noms crees");

        return created;
    }

    private static long Report(string what, long bytes)
    {
        Console.WriteLine($"      mesure  {what} : {bytes} octets");

        return bytes;
    }
}
