namespace Csharplings.Runner;

public enum EngineTarget
{
    Any,
    Godot,
    Unity,
}

public enum Profile
{
    All,
    Pure,
    Godot,
    Unity,
}

public static class Targets
{
    private static readonly HashSet<string> GodotSections = new(StringComparer.Ordinal)
    {
        "09_godot",
        "18_bridge",
    };

    private static readonly HashSet<string> UnitySections = new(StringComparer.Ordinal)
    {
        "19_unity",
    };

    private static readonly HashSet<string> GodotExercises = new(StringComparer.OrdinalIgnoreCase)
    {
        "order1",
        "cache1",
    };

    public static EngineTarget Of(Exercise exercise) =>
        GodotExercises.Contains(exercise.Id) ? EngineTarget.Godot : OfSection(exercise.Section);

    public static EngineTarget Of(Question question) => OfSection(question.Section);

    public static EngineTarget OfSection(string section)
    {
        if (GodotSections.Contains(section))
            return EngineTarget.Godot;

        if (UnitySections.Contains(section))
            return EngineTarget.Unity;

        return EngineTarget.Any;
    }

    public static bool Allows(Profile profile, EngineTarget target) =>
        profile switch
        {
            Profile.All => true,
            Profile.Pure => target == EngineTarget.Any,
            Profile.Godot => target != EngineTarget.Unity,
            _ => target != EngineTarget.Godot,
        };
}

public static class Config
{
    private const string FileName = ".csharplings-profile";

    private static Profile? _cached;

    public static Profile Current
    {
        get
        {
            _cached ??= Load();

            return _cached.Value;
        }
    }

    public static string Describe(Profile profile) =>
        profile switch
        {
            Profile.All => "tout, Godot et Unity melanges",
            Profile.Pure => "C# seul, aucun moteur",
            Profile.Godot => "C# et Godot",
            _ => "C# et Unity",
        };

    public static bool TryParse(string name, out Profile profile)
    {
        switch (name?.Trim().ToLowerInvariant())
        {
            case "all" or "tout":
                profile = Profile.All;
                return true;

            case "pure" or "csharp" or "c#":
                profile = Profile.Pure;
                return true;

            case "godot":
                profile = Profile.Godot;
                return true;

            case "unity":
                profile = Profile.Unity;
                return true;

            default:
                profile = Profile.All;
                return false;
        }
    }

    public static void Save(Profile profile)
    {
        File.WriteAllText(Path.Combine(Paths.Root, FileName), profile.ToString().ToLowerInvariant());
        _cached = profile;
    }

    private static Profile Load()
    {
        string file = Path.Combine(Paths.Root, FileName);

        if (File.Exists(file) && TryParse(File.ReadAllText(file), out Profile stored))
            return stored;

        return Profile.All;
    }
}
