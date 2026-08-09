namespace Csharplings;

public sealed record MonsterKind(string Id, int MaxHealth, int Damage, float Speed);

public static class MonsterDatabase
{
    private static readonly Dictionary<string, MonsterKind> Kinds = new()
    {
        ["gobelin"] = new MonsterKind("gobelin", 30, 4, 90f),
        ["golem"] = new MonsterKind("golem", 400, 25, 20f),
        ["chauve-souris"] = new MonsterKind("chauve-souris", 8, 1, 160f),
    };

    public static int Count => Kinds.Count;

    public static MonsterKind Get(string id)
    {
        return Todo.Value<MonsterKind>();
    }

    public static MonsterInstance Spawn(string id, Vector2 position) => new(Get(id), position);
}

public sealed class MonsterInstance
{
    public MonsterInstance(MonsterKind kind, Vector2 position)
    {
        Kind = new MonsterKind(kind.Id, kind.MaxHealth, kind.Damage, kind.Speed);
        Position = position;
        Health = kind.MaxHealth;
    }

    public MonsterKind Kind { get; }
    public Vector2 Position { get; set; }
    public int Health { get; private set; }

    public bool IsAlive => Health > 0;

    public void TakeDamage(int amount)
    {
        Health -= amount;
    }
}

public static class Data1
{
    public const bool NotDone = true;

    public static void Run()
    {
        Check.Equal(MonsterDatabase.Count, 3, "trois definitions dans la base");
        Check.Throws<KeyNotFoundException>(() => MonsterDatabase.Get("dragon"), "une definition absente se signale clairement");

        MonsterInstance first = MonsterDatabase.Spawn("gobelin", Vector2.Zero);
        MonsterInstance second = MonsterDatabase.Spawn("gobelin", new Vector2(50f, 0f));

        Check.Equal(first.Health, 30, "un monstre nait avec les points de vie de sa definition");
        Check.Equal(first.Kind.Damage, 4, "et lit ses stats dedans");
        Check.True(ReferenceEquals(first.Kind, second.Kind), "deux gobelins partagent LA definition, ils ne la copient pas");

        first.TakeDamage(12);

        Check.Equal(first.Health, 18, "les points de vie sont propres a chaque monstre");
        Check.Equal(second.Health, 30, "le voisin n'a rien senti");
        Check.Equal(first.Kind.MaxHealth, 30, "et la definition partagee n'a pas bouge");

        var horde = new List<MonsterInstance>();

        for (int i = 0; i < 500; i++)
            horde.Add(MonsterDatabase.Spawn("chauve-souris", new Vector2(i, 0f)));

        Check.True(horde.All(monster => ReferenceEquals(monster.Kind, horde[0].Kind)),
            "500 monstres a l'ecran, un seul objet de stats en memoire");

        horde[0].TakeDamage(1000);

        Check.False(horde[0].IsAlive, "les points de vie ne descendent pas sous zero");
        Check.Equal(horde[0].Health, 0, "ils s'arretent la");
        Check.True(horde[1].IsAlive, "et seul celui qu'on a frappe est mort");
    }
}
