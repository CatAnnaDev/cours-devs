namespace Csharplings;

public sealed class ParticleObject
{
    public Vector2 Position;
    public Vector2 Velocity;

    public void Step(float delta) => Position += Velocity * delta;
}

public sealed class ParticleColumns
{
    private readonly Vector2[] _positions;
    private readonly Vector2[] _velocities;

    public ParticleColumns(int count)
    {
        _positions = new Vector2[count];
        _velocities = new Vector2[count];

        for (int i = 0; i < count; i++)
            _velocities[i] = new Vector2(i % 7, 1f);
    }

    public void Step(float delta)
    {
        var next = new Vector2[_positions.Length];

        for (int i = 0; i < _positions.Length; i++)
            next[i] = _positions[i] + _velocities[i] * delta;

        Array.Copy(next, _positions, next.Length);
    }

    public Vector2 Total()
    {
        var total = Vector2.Zero;

        for (int i = 0; i < _positions.Length; i++)
            total += _positions[i];

        return total;
    }
}

public static class Bench1
{
    public const bool NotDone = true;

    private const int Count = 10_000;
    private const int Frames = 60;
    private const float Delta = 1f / 60f;

    private static object _keepsItAlive;

    public static void Run()
    {
        MeasureLayout();
        MeasureFrameCost();
        CompareResults();
        MeasureListGrowth();
    }

    private static void MeasureLayout()
    {
        Check.Equal(Report("un tableau de 10 000 Vector2", Allocations(() => _keepsItAlive = new Vector2[Count])), 80_024L,
            "UNE allocation de 80 024 octets : 24 d'en-tete plus 10 000 fois 8, d'un bloc et contigus en memoire");

        long objectBytes = Report("10 000 particules en objets", Allocations(() => _keepsItAlive = BuildObjects(Count)));
        long columnBytes = Report("les memes en deux colonnes", Allocations(() => _keepsItAlive = new ParticleColumns(Count)));

        Check.True(objectBytes > columnBytes * 2,
            "les objets coutent plus du double, et en 10 000 morceaux eparpilles sur le tas au lieu de deux blocs");
        Check.True(columnBytes < 200_000L,
            "les colonnes ne sont que deux blocs plus l'objet qui les tient");
    }

    private static void MeasureFrameCost()
    {
        var columns = new ParticleColumns(Count);
        List<ParticleObject> objects = BuildObjects(Count);
        ParticleObject[] asArray = objects.ToArray();

        Check.Equal(Report("une frame en colonnes", Allocations(() => columns.Step(Delta))), 0L,
            "faire avancer 10 000 particules en colonnes : ZERO octet alloue");
        Check.Equal(Report("une frame sur un tableau d'objets", Allocations(() => StepWithIndex(asArray, Delta))), 0L,
            "un tableau d'objets parcouru a l'index n'alloue rien non plus : le probleme n'est pas 'objet contre structure'");
        Check.Equal(Report("une frame en foreach sur List<T>", Allocations(() => StepWithListForeach(objects, Delta))), 0L,
            "un foreach sur une List<T> concrete non plus : son enumerateur est une structure");

        long throughInterface = Report("la meme frame derriere IEnumerable<T>", Allocations(() => StepThroughInterface(objects, Delta)));
        long throughLinq = Report("la meme frame en LINQ", Allocations(() => StepWithLinq(objects, Delta)));

        Check.True(throughInterface > 0L,
            "la MEME boucle derriere IEnumerable<T> emballe l'enumerateur dans un objet, a chaque appel");
        Check.True(throughLinq > throughInterface,
            "et LINQ alloue toute une chaine de traitement : 60 fois par seconde si tu le mets dans _Process");
    }

    private static void CompareResults()
    {
        var columns = new ParticleColumns(Count);
        ParticleObject[] particles = BuildObjects(Count).ToArray();

        for (int frame = 0; frame < Frames; frame++)
        {
            columns.Step(Delta);
            StepWithIndex(particles, Delta);
        }

        Vector2 fromColumns = columns.Total();
        Vector2 fromObjects = TotalOf(particles);

        Check.Near(fromColumns, fromObjects,
            "apres 60 frames les deux approches donnent le meme resultat : seul le cout change, jamais le jeu", 0.01);
        Check.Near(fromColumns.Y, Count * Frames * Delta,
            "et ce resultat est celui qu'on pouvait poser a la main", 0.5);
    }

    private static void MeasureListGrowth()
    {
        long withoutHint = Report("remplir une List<int> sans capacite", Allocations(() => _keepsItAlive = GrowWithoutHint(Count)));
        long withHint = Report("la meme avec la capacite annoncee", Allocations(() => _keepsItAlive = GrowWithHint(Count)));

        Check.True(withoutHint > withHint,
            "sans capacite annoncee, une List<T> double son tableau interne encore et encore : elle jette presque autant qu'elle garde");
        Check.True(withHint < 90_000L,
            "avec la capacite annoncee, elle alloue son tableau une seule fois");
    }

    private static long Report(string what, long bytes)
    {
        Console.WriteLine($"      mesure  {what} : {bytes} octets");

        return bytes;
    }

    private static List<ParticleObject> BuildObjects(int count)
    {
        var particles = new List<ParticleObject>(count);

        for (int i = 0; i < count; i++)
            particles.Add(new ParticleObject { Velocity = new Vector2(i % 7, 1f) });

        return particles;
    }

    private static void StepWithIndex(ParticleObject[] particles, float delta)
    {
        foreach (ParticleObject particle in particles.ToList())
            particle.Step(delta);
    }

    private static void StepWithListForeach(List<ParticleObject> particles, float delta)
    {
        foreach (ParticleObject particle in particles)
            particle.Step(delta);
    }

    private static void StepThroughInterface(IEnumerable<ParticleObject> particles, float delta)
    {
        foreach (ParticleObject particle in particles)
            particle.Step(delta);
    }

    private static void StepWithLinq(List<ParticleObject> particles, float delta)
    {
        foreach (ParticleObject particle in particles.Where(candidate => candidate.Velocity.Y > 0f))
            particle.Step(delta);
    }

    private static Vector2 TotalOf(ParticleObject[] particles)
    {
        var total = Vector2.Zero;

        for (int i = 0; i < particles.Length; i++)
            total += particles[i].Position;

        return total;
    }

    private static List<int> GrowWithoutHint(int count)
    {
        var values = new List<int>();

        for (int i = 0; i < count; i++)
            values.Add(i);

        return values;
    }

    private static List<int> GrowWithHint(int count)
    {
        var values = new List<int>();

        for (int i = 0; i < count; i++)
            values.Add(i);

        return values;
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
