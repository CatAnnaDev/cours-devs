namespace Csharplings;

public sealed class Emitter
{
    public event Action<int> Damaged;

    public int Failures { get; private set; }

    public int ListenerCount => Damaged is null ? 0 : 1;

    public void Hit(int amount) => Damaged?.Invoke(amount);

    public void HitSafely(int amount)
    {
        Damaged?.Invoke(amount);
    }
}

public static class Delegates2
{
    public const bool NotDone = true;

    public static long Measure(Action action)
    {
        action();
        action();
        action();

        long before = GC.GetAllocatedBytesForCurrentThread();
        action();

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    public static void Run()
    {
        Combat basic = (attack, defense) => attack - defense;

        Check.Equal(basic(10, 3), 7, "un 'delegate' declare s'utilise exactement comme un Func");

        var log = new List<string>();

        Action<int> chain = value => log.Add($"a{value}");
        chain += value => log.Add($"b{value}");
        chain(1);

        Check.Sequence(log, new[] { "a1", "b1" }, "un delegate multicast appelle TOUS ses abonnes, dans l'ordre d'ajout");

        Func<int> multi = () => 1;
        multi += () => 2;

        Check.Equal(multi(), 2,
            "sur un multicast qui renvoie une valeur, on ne recupere QUE celle du dernier : les autres sont jetees");

        var emitter = new Emitter();

        Check.Equal(emitter.ListenerCount, 0, "aucun abonne au depart");

        emitter.Damaged += value => log.Add("lambda");
        emitter.Damaged -= value => log.Add("lambda");

        Check.Equal(emitter.ListenerCount, 1,
            "'-=' avec une NOUVELLE lambda ne desabonne rien : c'est un autre objet, meme si le code est identique");

        void Handler(int value) => log.Add("methode");

        emitter.Damaged += Handler;

        Check.Equal(emitter.ListenerCount, 2, "on peut s'abonner avec un groupe de methodes");

        emitter.Damaged -= Handler;

        Check.Equal(emitter.ListenerCount, 1, "et la, le desabonnement marche : c'est la seule facon fiable");

        var risky = new Emitter();
        var seen = new List<string>();

        risky.Damaged += _ => seen.Add("premier");
        risky.Damaged += _ => throw new InvalidOperationException("boum");
        risky.Damaged += _ => seen.Add("troisieme");

        Check.Throws<InvalidOperationException>(() => risky.Hit(1), "une exception dans un abonne remonte a l'emetteur");
        Check.Sequence(seen, new[] { "premier" },
            "et TUE la chaine : le troisieme abonne n'a jamais tourne, sans que personne ne s'en apercoive");

        seen.Clear();
        risky.HitSafely(1);

        Check.Sequence(seen, new[] { "premier", "troisieme" }, "en parcourant la liste d'invocation, on isole les pannes");
        Check.Equal(risky.Failures, 1, "et on sait combien d'abonnes ont echoue");

        Check.Equal(
            Measure(() =>
            {
                Action noCapture = () => log.Add("rien");
            }),
            0L,
            "une lambda qui ne capture rien est fabriquee UNE fois et mise en cache : zero octet");

        Check.True(
            Measure(() =>
            {
                int local = log.Count;
                Action capturing = () => log.Add(local.ToString());
            }) > 0L,
            "mais des qu'elle capture une variable locale, il faut un objet pour la transporter : allocation a chaque passage");
    }
}
