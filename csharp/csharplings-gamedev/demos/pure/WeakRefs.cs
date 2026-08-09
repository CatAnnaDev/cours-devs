using System.Runtime.CompilerServices;
using Demos.WeakRefs;

namespace Demos.Pure;

public sealed class Payload
{
    public Payload(string name, int bytes = 1024)
    {
        Name = name;
        Blob = new byte[bytes];
    }

    public string Name { get; }

    public byte[] Blob { get; }
}

public sealed class Note
{
    public int Value { get; set; }
}

public sealed class Subscriber
{
    public int Heard { get; private set; }

    public void Count(int payload) => Heard += payload;
}

public static class WeakRefsDemo
{
    private static int _checks;
    private static int _failures;
    private static object _sink;

    public static void Demo()
    {
        Console.WriteLine("--- Les references faibles, verifiees une par une ---");
        Console.WriteLine();

        _checks = 0;
        _failures = 0;

        WeakReferenceClears();
        TableDoesNotHoldKey();
        WeakDictionaryLeaksWithoutSweep();
        CacheReturnsSameInstanceThenRebuilds();
        BusWithStaticLambdaLetsOwnerDie();
        BusWithCapturingLambdaKeepsOwnerAlive();
        BusDropsDeadSubscriptions();
        WhatItCosts();

        Console.WriteLine();
        Console.WriteLine($"  {_checks - _failures} / {_checks} affirmations verifiees");

        if (_failures > 0)
            Console.WriteLine($"  {_failures} ECHEC(S) : la documentation ment quelque part");

        Console.WriteLine();
    }

    private static void Check(bool condition, string claim)
    {
        _checks++;

        if (!condition)
            _failures++;

        Console.WriteLine($"  [{(condition ? "PASS" : "FAIL")}] {claim}");
    }

    private static void Collect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<Payload> MakeAndDrop(out bool joignableAvant)
    {
        var payload = new Payload("jetable");
        var weak = new WeakReference<Payload>(payload);

        joignableAvant = weak.TryGetTarget(out _);

        return weak;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference<Payload> Weak, Payload Strong) MakeAndKeep()
    {
        var payload = new Payload("gardee");

        return (new WeakReference<Payload>(payload), payload);
    }

    private static void WeakReferenceClears()
    {
        Console.WriteLine("1. WeakReference<T> : le principe");

        WeakReference<Payload> dropped = MakeAndDrop(out bool joignableAvant);

        Check(joignableAvant, "avant collecte, la cible est joignable");

        Collect();

        Check(!dropped.TryGetTarget(out _),
            "reference lachee puis collecte : la reference faible est vide. Elle n'a rien retenu");

        (WeakReference<Payload> weak, Payload strong) = MakeAndKeep();

        Collect();

        Check(weak.TryGetTarget(out Payload alive) && ReferenceEquals(alive, strong),
            "alors qu'une cible tenue par ailleurs survit a la collecte");

        Console.WriteLine();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<Payload> AddToTable(ConditionalWeakTable<Payload, Note> table, out bool present)
    {
        var key = new Payload("cle");

        table.GetOrCreateValue(key).Value = 7;

        var weak = new WeakReference<Payload>(key);

        present = weak.TryGetTarget(out _) && table.TryGetValue(key, out _);

        return weak;
    }

    private static void TableDoesNotHoldKey()
    {
        Console.WriteLine("2. ConditionalWeakTable : la cle n'est pas retenue");

        var table = new ConditionalWeakTable<Payload, Note>();
        var kept = new Payload("gardee");

        table.GetOrCreateValue(kept).Value = 1;

        WeakReference<Payload> weakKey = AddToTable(table, out bool presentAvant);

        Check(presentAvant, "la cle et son entree existent juste apres");

        Collect();

        Check(!weakKey.TryGetTarget(out _),
            "la table n'a PAS retenu sa cle : elle est collectee comme n'importe quel objet");
        Check(table.TryGetValue(kept, out Note note) && note.Value == 1,
            "et l'entree dont la cle vit encore est intacte");

        Console.WriteLine();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void FillCache(WeakCache<string, Payload> cache, int count)
    {
        for (int i = 0; i < count; i++)
            cache.Get($"asset{i}");
    }

    private static void WeakDictionaryLeaksWithoutSweep()
    {
        Console.WriteLine("3. Un dictionnaire de references faibles fuit ses cles");

        var cache = new WeakCache<string, Payload>(key => new Payload(key));

        FillCache(cache, 20);

        Collect();

        int deadBeforeSweep = cache.Sweep();

        Check(deadBeforeSweep > 0,
            $"apres collecte, {deadBeforeSweep} entrees mortes restaient dans le dictionnaire");
        Check(cache.Sweep() == 0, "le balayage suivant n'a plus rien a nettoyer");

        Console.WriteLine("        -> sans ce balayage, le cache anti-fuite devient la fuite");
        Console.WriteLine();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool GetTwiceAndDrop(WeakCache<string, Payload> cache)
    {
        Payload first = cache.Get("atlas");
        Payload second = cache.Get("atlas");

        return ReferenceEquals(first, second);
    }

    private static void CacheReturnsSameInstanceThenRebuilds()
    {
        Console.WriteLine("4. Le cache faible : meme instance tant que quelqu'un la tient");

        var built = 0;
        var cache = new WeakCache<string, Payload>(key =>
        {
            built++;

            return new Payload(key);
        });

        Check(GetTwiceAndDrop(cache), "deux appels rendent la MEME instance");
        Check(built == 1, "et la fabrique n'a tourne qu'une fois");

        Collect();

        cache.Get("atlas");

        Check(built == 2,
            "une fois la derniere reference forte lachee, le cache refabrique : il ne retenait rien");

        Console.WriteLine();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakEventBus<int> Bus, WeakReference<Subscriber> Weak) SubscribeWithStaticLambda()
    {
        var bus = new WeakEventBus<int>();
        var subscriber = new Subscriber();

        bus.Subscribe(subscriber, static (self, payload) => self.Count(payload));

        bus.Publish(1);

        return (bus, new WeakReference<Subscriber>(subscriber));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakEventBus<int> Bus, WeakReference<Subscriber> Weak) SubscribeWithCapturingLambda()
    {
        var bus = new WeakEventBus<int>();
        var subscriber = new Subscriber();

        bus.Subscribe(subscriber, (_, payload) => subscriber.Count(payload));

        return (bus, new WeakReference<Subscriber>(subscriber));
    }

    private static void BusWithStaticLambdaLetsOwnerDie()
    {
        Console.WriteLine("5. Bus faible avec une lambda 'static' : l'abonne peut mourir");

        (WeakEventBus<int> bus, WeakReference<Subscriber> weak) = SubscribeWithStaticLambda();

        Collect();

        Check(!weak.TryGetTarget(out _),
            "l'abonne est collecte alors que le bus est toujours la : le bus le tenait FAIBLEMENT");

        bus.Publish(1);

        Console.WriteLine("        -> publier ensuite ne plante pas");
        Console.WriteLine();
    }

    private static void BusWithCapturingLambdaKeepsOwnerAlive()
    {
        Console.WriteLine("6. Le MEME bus avec une lambda qui capture : l'abonne ne meurt plus");

        (WeakEventBus<int> bus, WeakReference<Subscriber> weak) = SubscribeWithCapturingLambda();

        Collect();

        bool stillAlive = weak.TryGetTarget(out Subscriber leaked);

        Check(stillAlive,
            "l'abonne SURVIT a la collecte : la lambda l'a capture, le delegate est tenu en fort par le bus");

        bus.Publish(5);

        Check(stillAlive && leaked.Heard == 5,
            "et il reagit encore, alors que plus personne d'autre ne le connait. Le motif est annule");

        Console.WriteLine("        -> la parade tient en un mot-cle : static devant la lambda");
        Console.WriteLine();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakEventBus<int> BusWithOneDoomedSubscriber(Subscriber kept)
    {
        var bus = new WeakEventBus<int>();
        var doomed = new Subscriber();

        bus.Subscribe(kept, static (self, payload) => self.Count(payload));
        bus.Subscribe(doomed, static (self, payload) => self.Count(payload));

        return bus;
    }

    private static void BusDropsDeadSubscriptions()
    {
        Console.WriteLine("7. Le bus se nettoie tout seul en publiant");

        var kept = new Subscriber();
        WeakEventBus<int> bus = BusWithOneDoomedSubscriber(kept);

        Collect();

        bus.Publish(3);

        Check(kept.Heard == 3, "l'abonne vivant recoit");

        bus.Publish(4);

        Check(kept.Heard == 7, "et continue de recevoir");

        Console.WriteLine("        -> l'abonne collecte a ete retire pendant la publication");
        Console.WriteLine();
    }

    private static void WhatItCosts()
    {
        Console.WriteLine("8. Ce que ca coute");

        var target = new Payload("cible", 16);

        long weakBytes = Allocations(() => _sink = new WeakReference<Payload>(target));
        long tableBytes = Allocations(() => _sink = new ConditionalWeakTable<Payload, Note>());

        Console.WriteLine($"        une WeakReference<T>      : {weakBytes} octets");
        Console.WriteLine($"        une ConditionalWeakTable  : {tableBytes} octets");

        Check(weakBytes > 0, "une reference faible est un OBJET : elle s'alloue");
        Check(weakBytes >= 24, "au moins l'en-tete d'un objet du tas, plus une poignee cote GC");

        Console.WriteLine("        -> sur un objet leger, la poignee coute plus cher que ce qu'elle observe");
        Console.WriteLine();
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
}
