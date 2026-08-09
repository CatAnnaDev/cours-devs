namespace Csharplings;

public interface IAudio
{
    List<string> Played { get; }

    void Play(string clip);
}

public sealed class RealAudio : IAudio
{
    public List<string> Played { get; } = new();

    public void Play(string clip) => Played.Add($"joue {clip}");
}

public sealed class SilentAudio : IAudio
{
    public List<string> Played { get; } = new();
}

public interface ISaveStore
{
    void Write(string key, int value);

    int Read(string key);
}

public sealed class MemorySaveStore : ISaveStore
{
    private readonly Dictionary<string, int> _values = new();

    public void Write(string key, int value) => _values[key] = value;

    public int Read(string key) => _values[key];
}

public static class Services
{
    private static readonly Dictionary<Type, object> Registry = new();

    public static void Register<T>(T service)
        where T : class
        => Registry[typeof(T)] = service;

    public static bool Has<T>()
        where T : class
        => Todo.Value<bool>();

    public static T Get<T>()
        where T : class
        => (T)Registry[typeof(T)];

    public static void Clear() => Registry.Clear();
}

public static class Locator1
{
    public const bool NotDone = true;

    public static void Explode()
    {
        Services.Get<RealAudio>().Play("boum");
        Services.Get<ISaveStore>().Write("explosions", Services.Get<ISaveStore>().Read("explosions") + 1);
    }

    public static void Run()
    {
        Services.Clear();

        Check.False(Services.Has<IAudio>(), "au demarrage, rien n'est enregistre");
        Check.Throws<InvalidOperationException>(() => Services.Get<IAudio>(),
            "demander un service absent doit dire clairement ce qui manque");

        var audio = new RealAudio();
        var store = new MemorySaveStore();

        Services.Register<IAudio>(audio);
        Services.Register<ISaveStore>(store);

        Check.True(Services.Has<IAudio>(), "le son est branche");
        Check.True(ReferenceEquals(Services.Get<IAudio>(), audio), "et on recupere bien l'instance donnee");

        Explode();
        Explode();

        Check.Sequence(audio.Played, new[] { "joue boum", "joue boum" }, "le code de jeu ne connait que l'interface");
        Check.Equal(store.Read("explosions"), 2, "et deux services differents cohabitent");

        var silent = new SilentAudio();
        Services.Register<IAudio>(silent);

        Explode();

        Check.Sequence(audio.Played, new[] { "joue boum", "joue boum" }, "l'ancien service ne recoit plus rien");
        Check.Equal(silent.Played.Count, 0, "et le nouveau reste muet : c'est ce qu'on veut dans un test");
        Check.Equal(store.Read("explosions"), 3, "pendant que le reste du jeu continue normalement");

        Services.Clear();

        Check.False(Services.Has<ISaveStore>(), "et on sait tout debrancher entre deux scenes");
    }
}
