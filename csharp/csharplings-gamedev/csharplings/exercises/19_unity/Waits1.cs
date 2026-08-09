using System.Collections;
using Csharplings.Unity;

namespace Csharplings;

public static class Waits1
{
    public const bool NotDone = true;

    private static readonly WaitForSeconds OneTenth = new(0.1f);

    private const float Frame = 1f / 60f;

    public static IEnumerator Wasteful(int beats)
    {
        for (int beat = 0; beat < beats; beat++)
            yield return new WaitForSeconds(0.1f);
    }

    public static IEnumerator Cached(int beats)
    {
        for (int beat = 0; beat < beats; beat++)
            yield return new WaitForSeconds(0.1f);
    }

    public static IEnumerator FrameByFrame(int frames)
    {
        for (int frame = 0; frame < frames; frame++)
            yield return new WaitForSeconds(0f);
    }

    public static void Run()
    {
        var runner = new CoroutineRunner();

        WaitForSeconds.ResetCounter();
        runner.Start(Wasteful(beats: 20));
        Drain(runner, 200);

        int wasteful = Report("20 attentes ecrites 'new WaitForSeconds(0.1f)'", WaitForSeconds.Created);

        Check.Equal(wasteful, 20,
            "chaque 'yield return new WaitForSeconds' fabrique un objet. Vingt battements, vingt objets, et une coroutine de boucle infinie en fabrique un par battement pour toujours");

        Check.Near(OneTenth.Seconds, 0.1, "l'attente partagee, elle, existe deja : elle a ete creee une fois pour toutes");

        WaitForSeconds.ResetCounter();
        runner.Start(Cached(beats: 20));
        Drain(runner, 200);

        int cached = Report("les memes attentes avec une instance en champ static readonly", WaitForSeconds.Created);

        Check.Equal(cached, 0,
            "une attente est une DUREE, pas un etat : une seule instance en 'static readonly' sert toute la partie");
        Check.True(wasteful > cached, "vingt objets contre zero, pour un comportement identique");

        WaitForSeconds.ResetCounter();
        runner.Start(FrameByFrame(frames: 30));
        Drain(runner, 200);

        Check.Equal(WaitForSeconds.Created, 0,
            "et 'yield return null' n'alloue rien du tout : c'est l'attente la moins chere, celle d'une frame");

        var timed = new CoroutineRunner();

        timed.Start(Cached(beats: 3));

        Check.Equal(timed.RunningCount, 1, "la coroutine tourne");

        for (int frame = 0; frame < 6; frame++)
            timed.Frame(Frame);

        Check.Equal(timed.RunningCount, 1, "six frames plus tard elle attend toujours son premier dixieme de seconde");

        for (int frame = 0; frame < 24; frame++)
            timed.Frame(Frame);

        Check.Equal(timed.RunningCount, 0,
            "et elle se termine apres ses trois battements : cacher l'attente ne change RIEN au timing, seulement a la facture memoire");

        var counting = new CoroutineRunner();

        counting.Start(FrameByFrame(frames: 5));

        for (int frame = 0; frame < 5; frame++)
            counting.Frame(Frame);

        Check.Equal(counting.RunningCount, 1, "cinq 'yield return null' consomment cinq frames");

        counting.Frame(Frame);

        Check.Equal(counting.RunningCount, 0, "la sixieme la termine");

        var busy = new CoroutineRunner();
        busy.Start(FrameByFrame(frames: 100_000));

        Check.Equal(Report("une frame de runner sur une coroutine deja lancee", Allocations(() => busy.Frame(Frame))), 0L,
            "avancer une coroutine deja demarree n'alloue rien : c'est son DEMARRAGE qui coute, plus ce qu'elle fabrique en route");
    }

    private static void Drain(CoroutineRunner runner, int frames)
    {
        for (int frame = 0; frame < frames; frame++)
            runner.Frame(Frame);
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

    private static int Report(string what, int count)
    {
        Console.WriteLine($"      mesure  {what} : {count} objets d'attente crees");

        return count;
    }

    private static long Report(string what, long bytes)
    {
        Console.WriteLine($"      mesure  {what} : {bytes} octets");

        return bytes;
    }
}
