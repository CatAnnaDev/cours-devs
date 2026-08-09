using Csharplings.Unity;

namespace Csharplings;

public sealed class LevelAssets : IDisposable
{
    private readonly List<AssetHandle> _handles = new();

    public int Count => _handles.Count;

    public AssetHandle Take(string path, int bytes)
    {
        AssetHandle handle = Addressables.Load(path, bytes);

        _handles.Add(handle);

        return handle;
    }

    public void Dispose()
    {
        _handles.Clear();
    }
}

public static class Load1
{
    public const bool NotDone = true;

    public const int Texture = 4_000_000;

    public static void LoadLeaky(int times)
    {
        for (int i = 0; i < times; i++)
            Addressables.Load("Textures/Sol", Texture);
    }

    public static void LoadClean(int times)
    {
        for (int i = 0; i < times; i++)
        {
            using var assets = new LevelAssets();

            assets.Take("Textures/Sol", Texture);
        }
    }

    public static void Run()
    {
        Addressables.Reset();

        AssetHandle first = Addressables.Load("Textures/Sol", Texture);

        Check.Equal(Addressables.LiveBytes, Texture, "un chargement met la texture en memoire");
        Check.Equal(Addressables.ReferenceCountOf("Textures/Sol"), 1, "avec un compteur de references a un");

        AssetHandle second = Addressables.Load("Textures/Sol", Texture);

        Check.Equal(Addressables.LiveBytes, Texture,
            "la charger une seconde fois ne double PAS la memoire : c'est la meme texture, le moteur la partage");

        Check.Equal(Addressables.ReferenceCountOf("Textures/Sol"), 2, "il compte simplement deux demandeurs");

        Addressables.Release(first);

        Check.Equal(Addressables.LiveBytes, Texture,
            "en liberer une n'a rien libere du tout : quelqu'un s'en sert encore, et c'est tout l'interet du comptage");

        Addressables.Release(second);

        Check.Equal(Addressables.LiveBytes, 0, "il faut autant de Release que de Load. Le dernier libere vraiment");
        Check.Equal(Addressables.ReferenceCountOf("Textures/Sol"), 0, "et le compteur retombe a zero");

        Addressables.Release(second);

        Check.Equal(Addressables.LiveBytes, 0,
            "un Release en trop ne doit RIEN faire : sans ce garde, le compteur passe sous zero et la texture est liberee alors qu'un autre niveau l'utilise. Le plantage sort ailleurs, plus tard");

        Addressables.Reset();
        LoadLeaky(10);

        Check.Equal(Addressables.LoadCalls, 10, "dix chargements");
        Check.Equal(Addressables.ReferenceCountOf("Textures/Sol"), 10,
            "dix references, zero liberation : c'est la fuite classique, une scene rechargee dix fois qui garde dix prises sur les memes assets");

        Check.Equal(Addressables.LiveBytes, Texture,
            "la memoire ne monte pas ici parce que c'est le MEME chemin. Avec dix textures differentes, ce serait quarante megaoctets qui ne repartiront jamais");

        Addressables.Reset();
        LoadClean(10);

        Check.Equal(Addressables.LoadCalls, 10, "dix chargements aussi");
        Check.Equal(Addressables.LiveBytes, 0,
            "mais zero octet retenu : chaque niveau rend ce qu'il a pris. Un IDisposable qui garde ses prises et les libere toutes, et un 'using' qui l'appelle meme si une exception traverse le chargement");

        Check.Equal(Addressables.ReferenceCountOf("Textures/Sol"), 0, "aucune reference en suspens");

        Addressables.Reset();

        using (var level = new LevelAssets())
        {
            level.Take("Textures/Sol", Texture);
            level.Take("Sons/Ambiance", 1_000_000);

            Check.Equal(level.Count, 2, "un niveau tient la liste de ce qu'il a demande");
            Check.Equal(Addressables.LiveBytes, Texture + 1_000_000, "et la memoire suit");
        }

        Check.Equal(Addressables.LiveBytes, 0,
            "a la sortie du bloc, tout est rendu. C'est la seule facon de charger qui tienne sur la duree : celui qui charge est celui qui libere, et il le fait dans un Dispose");
    }
}
