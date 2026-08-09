using System.Reflection;

namespace Csharplings;

[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
public sealed class ModuleAttribute : Attribute
{
    public ModuleAttribute(string id)
    {
        Id = id;
    }

    public string Id { get; }

    public int Order { get; init; }

    public bool Experimental { get; init; }
}

[Module("audio", Order = 10)]
public sealed class AudioModule
{
}

[Module("render", Order = 1)]
public sealed class RenderModule
{
}

[Module("net", Order = 5, Experimental = true)]
public sealed class NetModule
{
}

public sealed class PlainModule
{
}

public static class Attrs1
{
    public const bool NotDone = true;

    public static List<(string Id, int Order)> Modules(bool includeExperimental) =>
        typeof(Attrs1).Assembly
            .GetTypes()
            .Select(type => (Type: type, Attribute: type.GetCustomAttribute<ModuleAttribute>()))
            .Where(pair => pair.Attribute is not null)
            .Where(pair => includeExperimental || !pair.Attribute.Experimental)
            .OrderBy(pair => pair.Attribute.Id, StringComparer.Ordinal)
            .Select(pair => (pair.Attribute.Id, pair.Attribute.Order))
            .ToList();

    public static string DuplicateId()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach ((string id, int _) in Modules(includeExperimental: true))
        {
            if (!seen.Add(id))
                return id;
        }

        return null;
    }

    public static void Run()
    {
        Check.Sequence(Modules(includeExperimental: true).Select(module => module.Id), new[] { "render", "net", "audio" },
            "un attribut porte des DONNEES a cote du type : ici un identifiant et un ordre de chargement, lus au demarrage");

        Check.Sequence(Modules(includeExperimental: false).Select(module => module.Id), new[] { "render", "audio" },
            "et une propriete facultative sert de filtre : les modules experimentaux ne sortent que si on les demande");

        Check.Equal(Modules(includeExperimental: true).Count, 3, "trois modules marques");

        Check.True(typeof(PlainModule).GetCustomAttribute<ModuleAttribute>() is null,
            "un type non marque rend null : GetCustomAttribute ne leve jamais pour une absence");

        ModuleAttribute audio = typeof(AudioModule).GetCustomAttribute<ModuleAttribute>();

        Check.Equal(audio.Id, "audio", "l'argument du constructeur est obligatoire : c'est ce qu'on met la quand la donnee ne peut pas manquer");
        Check.Equal(audio.Order, 10, "et une propriete 'init' est facultative, avec une valeur par defaut");
        Check.False(audio.Experimental, "celle-ci vaut false sans qu'on l'ait ecrit");

        Check.True(DuplicateId() is null,
            "et voila le VRAI usage : verifier au demarrage que le contrat tient. Ici, qu'aucun identifiant n'est en double");

        Check.Equal(typeof(ModuleAttribute).GetCustomAttribute<AttributeUsageAttribute>().ValidOn, AttributeTargets.Class,
            "AttributeUsage limite ou l'attribut peut se poser : le compilateur refuse alors de le mettre ailleurs, ce qui est une verification gratuite");

        Check.False(typeof(ModuleAttribute).GetCustomAttribute<AttributeUsageAttribute>().Inherited,
            "et Inherited a false evite qu'une classe fille herite silencieusement de l'identifiant de sa mere, ce qui donnerait deux modules du meme nom");

        Check.Equal(Modules(includeExperimental: true)[0].Order, 1,
            "l'ordre vient de l'attribut, pas de l'ordre de decouverte : c'est ce qui rend le chargement reproductible d'une compilation a l'autre");

        Check.True(typeof(AudioModule).IsDefined(typeof(ModuleAttribute), inherit: false),
            "IsDefined repond juste oui ou non, sans construire l'attribut : c'est la version economique quand on filtre des milliers de types");
    }
}
