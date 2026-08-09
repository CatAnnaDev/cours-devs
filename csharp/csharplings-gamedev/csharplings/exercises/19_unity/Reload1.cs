using Csharplings.Unity;

namespace Csharplings;

public static class RunStats
{
    public static int Kills;

    public static readonly List<string> Log = new();

    public static event Action<int> KillReached;

    public static int SubscriberCount => KillReached?.GetInvocationList().Length ?? 0;

    public static void Register(int kills)
    {
        Kills += kills;
        Log.Add($"+{kills}");
        KillReached?.Invoke(Kills);
    }

    public static void ResetForNewSession()
    {
        Kills = 0;
    }

    public static void Subscribe(Action<int> handler) => KillReached += handler;
}

public sealed class ScoreWidget : MonoBehaviour
{
    public int Notifications { get; private set; }

    public override void OnEnable() => RunStats.Subscribe(OnKillReached);

    private void OnKillReached(int total) => Notifications++;
}

public static class Reload1
{
    public const bool NotDone = true;

    public static Scene EnterPlayMode()
    {
        RunStats.ResetForNewSession();

        return new Scene();
    }

    public static void Run()
    {
        Scene first = EnterPlayMode();
        ScoreWidget widget = first.Add(new ScoreWidget());

        RunStats.Register(3);
        RunStats.Register(2);

        Check.Equal(RunStats.Kills, 5, "premiere partie : cinq victimes");
        Check.Equal(RunStats.Log.Count, 2, "deux entrees dans le journal");
        Check.Equal(widget.Notifications, 2, "et le widget a ete prevenu deux fois");
        Check.Equal(RunStats.SubscriberCount, 1, "il y a un seul abonne a l'evenement statique");

        Scene second = EnterPlayMode();

        Check.Equal(RunStats.Kills, 0,
            "au lancement d'une nouvelle partie, il faut remettre les statiques a zero A LA MAIN : le rechargement de domaine est desactive chez la plupart des equipes, pour gagner du temps de compilation");
        Check.Equal(RunStats.Log.Count, 0, "journal vide");
        Check.Equal(RunStats.SubscriberCount, 0,
            "et surtout : l'evenement statique est vide. Sans ca, le widget de la partie PRECEDENTE y serait encore abonne");

        ScoreWidget freshWidget = second.Add(new ScoreWidget());

        RunStats.Register(1);

        Check.Equal(RunStats.SubscriberCount, 1, "un seul abonne : celui de la partie en cours");
        Check.Equal(freshWidget.Notifications, 1, "le nouveau widget est prevenu une fois");
        Check.Equal(widget.Notifications, 2,
            "et l'ancien N'A PAS bouge. C'est tout l'enjeu : un evenement statique jamais vide fait travailler des objets morts, et le compteur monte deux fois, puis trois, puis dix");

        Scene third = new Scene();
        ScoreWidget leaking = third.Add(new ScoreWidget());

        RunStats.Register(1);

        Check.Equal(RunStats.SubscriberCount, 2,
            "voila ce qui se passe si on entre en jeu SANS remettre a zero : deux abonnes pour un seul widget vivant");
        Check.Equal(leaking.Notifications, 1, "le nouveau compte une fois");
        Check.Equal(freshWidget.Notifications, 2,
            "mais l'ancien aussi, alors que sa partie est terminee : il repond encore, et il maintient son objet en vie par-dessus le marche");
        Check.Equal(RunStats.Kills, 2, "et le compteur a repris la ou il en etait au lieu de repartir de zero");

        RunStats.ResetForNewSession();

        Check.Equal(RunStats.SubscriberCount, 0, "un reset explicite au demarrage de chaque partie regle les deux problemes d'un coup");
        Check.Equal(RunStats.Kills, 0, "l'etat");
        Check.True(RunStats.Log.Count == 0, "et le journal");
    }
}
