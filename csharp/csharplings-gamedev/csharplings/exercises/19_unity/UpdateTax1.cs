using Csharplings.Unity;

namespace Csharplings;

public sealed class SoloEnemy : MonoBehaviour
{
    public float Health = 100f;

    public override void Update() => Health -= 1f;
}

public sealed class EnemyState
{
    public float Health = 100f;

    public void Step() => Health -= 1f;
}

public sealed class EnemyManager : MonoBehaviour
{
    private readonly List<EnemyState> _enemies;

    public EnemyManager(int count)
    {
        _enemies = new List<EnemyState>(count);

        for (int i = 0; i < count; i++)
            _enemies.Add(new EnemyState());
    }

    public IReadOnlyList<EnemyState> Enemies => _enemies;

    public override void Update()
    {
        foreach (EnemyState enemy in Enemies)
            enemy.Step();
    }
}

public static class UpdateTax1
{
    public const bool NotDone = true;

    private const int Count = 1000;
    private const int Frames = 10;

    public static void Run()
    {
        var solo = new Scene();
        var enemies = new List<SoloEnemy>(Count);

        for (int i = 0; i < Count; i++)
            enemies.Add(solo.Add(new SoloEnemy()));

        MonoBehaviour.EngineCallbacks = 0;
        solo.Frames(Frames);

        int oneScriptEach = Report("1000 scripts avec chacun son Update, sur 10 frames", MonoBehaviour.EngineCallbacks);

        Check.Equal(oneScriptEach, Count * Frames,
            "dix mille passages du moteur vers ton code : chacun est un franchissement de la frontiere natif vers manage, et il n'est jamais gratuit");
        Check.Near(enemies[0].Health, 90.0, "les ennemis ont bien perdu dix points");

        var managed = new Scene();
        var manager = managed.Add(new EnemyManager(Count));

        MonoBehaviour.EngineCallbacks = 0;
        managed.Frames(Frames);

        int oneManager = Report("un seul manager qui boucle sur 1000 ennemis, sur 10 frames", MonoBehaviour.EngineCallbacks);

        Check.Equal(oneManager, Frames,
            "dix passages. Le moteur appelle UN script, qui fait la boucle lui-meme : c'est le 'manager pattern', et ce n'est pas un style, c'est une necessite a l'echelle");
        Check.Equal(oneScriptEach / oneManager, Count, "mille fois moins d'allers-retours pour le meme travail");

        Check.Near(manager.Enemies[0].Health, 90.0, "et le resultat est identique au point pres");
        Check.Equal(manager.Enemies.Count, Count, "sur le meme nombre d'ennemis");

        Check.Equal(managed.BehaviourCount, 1,
            "cote scene, il n'y a plus qu'un seul objet a suivre au lieu de mille");
        Check.Equal(solo.BehaviourCount, Count, "contre mille de l'autre cote");

        Check.Equal(Allocations(() => managed.Frame()), 0L,
            "et la boucle du manager n'alloue rien : la version groupee n'echange pas de la memoire contre du temps");

        var mixed = new Scene();
        var sleeping = mixed.Add(new SoloEnemy());

        mixed.Add(new SoloEnemy());
        sleeping.SetEnabled(true);

        MonoBehaviour.EngineCallbacks = 0;
        mixed.Frames(Frames);

        Check.Equal(MonoBehaviour.EngineCallbacks, Frames,
            "desactiver un script suffit a supprimer son appel : c'est la version pauvre de la meme optimisation");
        Check.Near(sleeping.Health, 100.0, "et un script desactive ne travaille plus du tout");
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

    private static int Report(string what, int calls)
    {
        Console.WriteLine($"      mesure  {what} : {calls} appels du moteur");

        return calls;
    }
}
