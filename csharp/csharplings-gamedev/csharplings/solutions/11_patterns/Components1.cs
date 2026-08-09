namespace Csharplings;

public abstract class Component
{
    public GameObject Owner { get; internal set; }

    public virtual void Tick(float delta) { }
}

public sealed class GameObject
{
    private readonly List<Component> _components = new();

    public GameObject(string name)
    {
        Name = name;
    }

    public string Name { get; }
    public Vector2 Position { get; set; }

    public T Add<T>(T component)
        where T : Component
    {
        component.Owner = this;
        _components.Add(component);

        return component;
    }

    public T Get<T>()
        where T : Component
        => _components.OfType<T>().FirstOrDefault();

    public bool Has<T>()
        where T : Component
        => Get<T>() is not null;

    public void Tick(float delta)
    {
        foreach (Component component in _components.ToList())
            component.Tick(delta);
    }
}

public sealed class Velocity : Component
{
    public Vector2 Value { get; set; }

    public override void Tick(float delta)
    {
        Owner.Position += Value * delta;
    }
}

public sealed class Health : Component
{
    public int Current { get; set; } = 100;

    public bool IsDead => Current <= 0;
}

public sealed class Poison : Component
{
    public float Elapsed { get; private set; }

    public override void Tick(float delta)
    {
        Elapsed += delta;

        Health health = Owner.Get<Health>();

        if (health is not null)
            health.Current -= 1;
    }
}

public static class Components1
{
    public const bool NotDone = false;

    public static void Run()
    {
        var player = new GameObject("joueur");
        Velocity velocity = player.Add(new Velocity { Value = new Vector2(100f, 0f) });
        player.Add(new Health());

        Check.True(player.Has<Velocity>(), "le joueur bouge");
        Check.True(player.Has<Health>(), "et peut mourir");
        Check.False(player.Has<Poison>(), "mais n'est pas empoisonne");
        Check.True(ReferenceEquals(velocity.Owner, player), "un composant connait son porteur");

        player.Tick(0.5f);

        Check.Near(player.Position, new Vector2(50f, 0f), "le composant de vitesse deplace son porteur");
        Check.Equal(player.Get<Health>().Current, 100, "les autres composants ne s'en melent pas");

        var wall = new GameObject("mur");
        wall.Add(new Health { Current = 30 });
        wall.Tick(10f);

        Check.Near(wall.Position, Vector2.Zero, "un mur sans vitesse ne bouge pas : rien a desactiver, il n'a pas le composant");
        Check.Equal(wall.Get<Velocity>(), null, "Get rend null quand le composant n'est pas la");

        player.Add(new Poison());
        player.Tick(1f);
        player.Tick(1f);

        Check.Equal(player.Get<Health>().Current, 98, "un composant ajoute a chaud agit des la frame suivante");
        Check.Near(player.Get<Poison>().Elapsed, 2.0, "et garde son propre etat");

        var stone = new GameObject("pierre");
        stone.Add(new Poison());
        stone.Tick(1f);

        Check.Equal(stone.Get<Poison>().Elapsed, 1f, "un composant doit survivre a l'absence de ses voisins");
    }
}
