namespace Csharplings;

public sealed class Texture : IDisposable
{
    private readonly List<string> _log;

    public Texture(string name, List<string> log)
    {
        Name = name;
        _log = log;
        _log.Add($"charge {name}");
    }

    public string Name { get; }
    public bool Released { get; private set; }

    public void Dispose()
    {
        if (Released)
            return;

        Released = true;
        _log.Add($"libere {Name}");
    }
}

public static class Disposable1
{
    public const bool NotDone = false;

    public static void LoadLevel(List<string> log)
    {
        using var tileset = new Texture("tileset", log);
        using var sprites = new Texture("sprites", log);

        log.Add("niveau pret");
    }

    public static void LoadCorrupted(List<string> log)
    {
        using var atlas = new Texture("atlas", log);

        throw new InvalidOperationException("fichier corrompu");
    }

    public static void Run()
    {
        var log = new List<string>();
        LoadLevel(log);

        Check.Sequence(log,
            new[] { "charge tileset", "charge sprites", "niveau pret", "libere sprites", "libere tileset" },
            "on libere a la sortie du bloc, dans l'ordre inverse du chargement");

        var crash = new List<string>();

        Check.Throws<InvalidOperationException>(() => LoadCorrupted(crash), "l'exception remonte quand meme");
        Check.Sequence(crash, new[] { "charge atlas", "libere atlas" }, "mais 'using' libere avant de la laisser passer");

        var hud = new List<string>();
        var texture = new Texture("hud", hud);

        texture.Dispose();
        texture.Dispose();

        Check.True(texture.Released, "l'objet sait qu'il est libere");
        Check.Sequence(hud, new[] { "charge hud", "libere hud" }, "et Dispose doit etre sans effet la deuxieme fois");
    }
}
