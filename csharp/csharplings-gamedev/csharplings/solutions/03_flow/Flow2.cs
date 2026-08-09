namespace Csharplings;

public enum Mood
{
    Idle,
    Chase,
    Flee,
}

public static class Flow2
{
    public const bool NotDone = false;

    public static string Describe(Mood mood) => mood switch
    {
        Mood.Idle => "je me repose",
        Mood.Chase => "je te poursuis",
        Mood.Flee => "je m'enfuis",
        _ => "inconnu",
    };

    public static int Priority(Mood mood) => mood switch
    {
        Mood.Flee => 2,
        Mood.Chase => 1,
        _ => 0,
    };

    public static void Run()
    {
        Check.Equal(Describe(Mood.Idle), "je me repose", "l'humeur Idle");
        Check.Equal(Describe(Mood.Chase), "je te poursuis", "l'humeur Chase");
        Check.Equal(Describe(Mood.Flee), "je m'enfuis", "l'humeur Flee");

        Check.Equal(Priority(Mood.Flee), 2, "fuir est prioritaire");
        Check.Equal(Priority(Mood.Chase), 1, "poursuivre passe apres");
        Check.Equal(Priority(Mood.Idle), 0, "le repos n'est pas prioritaire");
    }
}
