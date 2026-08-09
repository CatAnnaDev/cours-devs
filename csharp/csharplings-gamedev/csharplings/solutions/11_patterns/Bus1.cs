namespace Csharplings;

public readonly record struct EnemyKilled(string Name, int Points);

public readonly record struct LevelLoaded(string Scene);

public sealed class EventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();

    public void Subscribe<T>(Action<T> handler)
    {
        if (!_handlers.TryGetValue(typeof(T), out List<Delegate> listeners))
        {
            listeners = new List<Delegate>();
            _handlers[typeof(T)] = listeners;
        }

        listeners.Add(handler);
    }

    public void Unsubscribe<T>(Action<T> handler)
    {
        if (_handlers.TryGetValue(typeof(T), out List<Delegate> listeners))
            listeners.Remove(handler);
    }

    public int Publish<T>(T message)
    {
        if (!_handlers.TryGetValue(typeof(T), out List<Delegate> listeners))
            return 0;

        foreach (Delegate handler in listeners.ToList())
            ((Action<T>)handler)(message);

        return listeners.Count;
    }
}

public static class Bus1
{
    public const bool NotDone = false;

    public static void Run()
    {
        var bus = new EventBus();
        var score = new List<int>();
        var quests = new List<string>();
        var scenes = new List<string>();

        void OnKillScore(EnemyKilled killed) => score.Add(killed.Points);
        void OnKillQuest(EnemyKilled killed) => quests.Add(killed.Name);
        void OnLevel(LevelLoaded loaded) => scenes.Add(loaded.Scene);

        Check.Equal(bus.Publish(new EnemyKilled("rat", 1)), 0, "publier sans abonne ne doit pas planter");

        bus.Subscribe<EnemyKilled>(OnKillScore);
        bus.Subscribe<EnemyKilled>(OnKillQuest);
        bus.Subscribe<LevelLoaded>(OnLevel);

        Check.Equal(bus.Publish(new EnemyKilled("gobelin", 12)), 2, "deux systemes reagissent au meme message");
        Check.Sequence(score, new[] { 12 }, "le score encaisse les points");
        Check.Sequence(quests, new[] { "gobelin" }, "la quete note le nom");
        Check.Equal(scenes.Count, 0, "et le chargeur de niveau n'a rien vu : le type sert d'adresse");

        bus.Publish(new LevelLoaded("donjon"));

        Check.Sequence(scenes, new[] { "donjon" }, "chaque type de message a ses propres abonnes");
        Check.Sequence(score, new[] { 12 }, "les autres ne bougent pas");

        bus.Unsubscribe<EnemyKilled>(OnKillQuest);
        bus.Publish(new EnemyKilled("golem", 40));

        Check.Sequence(score, new[] { 12, 40 }, "le score suit toujours");
        Check.Sequence(quests, new[] { "gobelin" }, "la quete s'est desabonnee");

        Check.Equal(bus.Publish(new LevelLoaded("ville")), 1, "Publish rend le nombre d'abonnes touches");
    }
}
