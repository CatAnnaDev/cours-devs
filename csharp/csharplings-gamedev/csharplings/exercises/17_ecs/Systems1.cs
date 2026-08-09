namespace Csharplings;

public sealed class Simulation
{
    private const int Capacity = 32;

    private readonly Vector2[] _positions = new Vector2[Capacity];
    private readonly Vector2[] _velocities = new Vector2[Capacity];
    private readonly bool[] _alive = new bool[Capacity];

    public int SlotCount { get; private set; }

    public int AliveCount { get; private set; }

    public int Spawn(Vector2 position, Vector2 velocity)
    {
        if (SlotCount == Capacity)
            throw new InvalidOperationException("simulation pleine");

        int entity = SlotCount++;

        _positions[entity] = position;
        _velocities[entity] = velocity;
        _alive[entity] = true;
        AliveCount++;

        return entity;
    }

    public void Destroy(int entity)
    {
        if (!IsAlive(entity))
            return;

        _alive[entity] = false;
        AliveCount--;
    }

    public bool IsAlive(int entity) => entity >= 0 && entity < SlotCount && _alive[entity];

    public ref Vector2 Position(int entity) => ref _positions[entity];

    public ref Vector2 Velocity(int entity) => ref _velocities[entity];
}

public sealed class CommandBuffer
{
    private readonly List<int> _toDestroy = new();
    private readonly List<Vector2> _spawnPositions = new();
    private readonly List<Vector2> _spawnVelocities = new();

    public int PendingCount => _toDestroy.Count + _spawnPositions.Count;

    public void Destroy(int entity) => _toDestroy.Add(entity);

    public void Spawn(Vector2 position, Vector2 velocity)
    {
        _spawnPositions.Add(position);
        _spawnVelocities.Add(velocity);
    }

    public void Apply(Simulation simulation)
    {
        for (int i = 0; i < _toDestroy.Count; i++)
            simulation.Destroy(_toDestroy[i]);

        for (int i = 0; i < _spawnPositions.Count; i++)
            simulation.Spawn(_spawnPositions[i], _spawnVelocities[i]);

    }
}

public interface ISimulationSystem
{
    void Update(Simulation simulation, CommandBuffer commands, float delta);
}

public sealed class DespawnSystem : ISimulationSystem
{
    public void Update(Simulation simulation, CommandBuffer commands, float delta)
    {
        for (int entity = 0; entity < simulation.SlotCount; entity++)
        {
            if (simulation.IsAlive(entity) && simulation.Position(entity).X > 90f)
                commands.Destroy(entity);
        }
    }
}

public sealed class TrailSystem : ISimulationSystem
{
    public void Update(Simulation simulation, CommandBuffer commands, float delta)
    {
        for (int entity = 0; entity < simulation.SlotCount; entity++)
        {
            if (simulation.IsAlive(entity) && simulation.Velocity(entity).X > 40f)
                simulation.Spawn(simulation.Position(entity), simulation.Velocity(entity) * 0.5f);
        }
    }
}

public sealed class MovementSystem : ISimulationSystem
{
    public void Update(Simulation simulation, CommandBuffer commands, float delta)
    {
        for (int entity = 0; entity < simulation.SlotCount; entity++)
        {
            if (simulation.IsAlive(entity))
                simulation.Position(entity) += simulation.Velocity(entity) * delta;
        }
    }
}

public sealed class SystemPipeline
{
    private readonly ISimulationSystem[] _systems;
    private readonly CommandBuffer _commands = new();

    public SystemPipeline(params ISimulationSystem[] systems)
    {
        _systems = systems;
    }

    public int PendingCount => _commands.PendingCount;

    public void Frame(Simulation simulation, float delta)
    {
        _commands.Apply(simulation);

        for (int i = 0; i < _systems.Length; i++)
            _systems[i].Update(simulation, _commands, delta);
    }
}

public static class Systems1
{
    public const bool NotDone = true;

    public static void Run()
    {
        var simulation = new Simulation();

        int fast = simulation.Spawn(Vector2.Zero, new Vector2(50f, 0f));
        int slow = simulation.Spawn(Vector2.Zero, new Vector2(10f, 0f));

        var pipeline = new SystemPipeline(new DespawnSystem(), new TrailSystem(), new MovementSystem());

        pipeline.Frame(simulation, 1f);

        Check.Near(simulation.Position(fast), new Vector2(50f, 0f), "le rapide a avance de sa vitesse fois delta");
        Check.Near(simulation.Position(slow), new Vector2(10f, 0f), "le lent aussi, chacun a son rythme");
        Check.Equal(simulation.SlotCount, 3, "la trainee du rapide est nee : trois slots occupes");
        Check.Equal(simulation.AliveCount, 3, "et trois entites en vie");
        Check.Equal(pipeline.PendingCount, 0,
            "le tampon a ete vide en l'appliquant : sinon les memes commandes repartiraient a la frame suivante");

        int trail = 2;

        Check.Near(simulation.Position(trail), Vector2.Zero,
            "la trainee est nee la ou etait le rapide, et n'a PAS bouge dans sa frame de naissance : elle n'existait pas quand le mouvement a tourne");
        Check.Near(simulation.Velocity(trail), new Vector2(25f, 0f), "elle herite de la moitie de la vitesse");

        pipeline.Frame(simulation, 1f);

        Check.Near(simulation.Position(fast), new Vector2(100f, 0f), "deuxieme frame, le rapide est a 100");
        Check.Near(simulation.Position(trail), new Vector2(25f, 0f), "et la trainee bouge a partir de la frame suivante");
        Check.Equal(simulation.SlotCount, 4, "le rapide a laisse une deuxieme trainee");
        Check.Near(simulation.Position(3), new Vector2(50f, 0f), "nee la ou le rapide etait au debut de la frame");

        pipeline.Frame(simulation, 1f);

        Check.False(simulation.IsAlive(fast), "troisieme frame : le rapide a depasse 90, il est detruit");
        Check.Near(simulation.Position(fast), new Vector2(150f, 0f),
            "mais il a TERMINE sa frame avant de mourir : la destruction n'a pris effet qu'a la fin, donc tous les systemes de la frame ont vu le meme monde");
        Check.Equal(simulation.AliveCount, 4, "une entite en moins, une trainee en plus");
        Check.Equal(simulation.SlotCount, 5, "et cinq slots utilises depuis le debut");
        Check.Near(simulation.Position(slow), new Vector2(30f, 0f), "le lent continue tranquillement");

        pipeline.Frame(simulation, 1f);

        Check.Near(simulation.Position(fast), new Vector2(150f, 0f), "une entite morte ne bouge plus : les systemes l'ignorent");

        var buffer = new CommandBuffer();
        var solo = new Simulation();
        int only = solo.Spawn(Vector2.Zero, Vector2.Zero);

        buffer.Destroy(only);
        buffer.Destroy(only);

        Check.Equal(buffer.PendingCount, 2, "deux commandes en attente");
        Check.True(solo.IsAlive(only), "et rien n'a encore change dans le monde : c'est tout l'interet du differe");

        buffer.Apply(solo);

        Check.False(solo.IsAlive(only), "l'application fait le travail");
        Check.Equal(solo.AliveCount, 0, "detruire deux fois la meme entite dans la meme frame est sans danger");
        Check.Equal(buffer.PendingCount, 0, "et le tampon est vide");

        buffer.Apply(solo);

        Check.Equal(solo.SlotCount, 1, "reappliquer un tampon vide ne fabrique rien");
    }
}
