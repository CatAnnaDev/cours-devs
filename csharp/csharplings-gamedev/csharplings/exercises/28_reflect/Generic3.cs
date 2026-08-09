using System.Reflection;

namespace Csharplings;

public interface IComponentStore
{
    int Count { get; }

    void AddBoxed(object component);
}

public sealed class ComponentStore<T> : IComponentStore
{
    private readonly List<T> _items = new();

    public int Count => _items.Count;

    public void Add(T component) => _items.Add(component);

    public void AddBoxed(object component) => Add((T)component);

    public T this[int index] => _items[index];
}

public static class Generic3
{
    public const bool NotDone = true;

    private static readonly Dictionary<Type, IComponentStore> Stores = new();

    public static IComponentStore StoreFor(Type componentType)
    {
        Type closed = typeof(ComponentStore<>).MakeGenericType(componentType);
        var created = (IComponentStore)Activator.CreateInstance(closed);

        Stores[componentType] = created;

        return created;
    }

    public static int CountVia(Type componentType) => StoreFor(componentType).Count;

    public static object CallGeneric(string methodName, Type argument, object[] parameters)
    {
        MethodInfo open = typeof(Generic3).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);

        return open.Invoke(null, parameters);
    }

    public static string Describe<T>(T value) => typeof(T).Name + ":" + value;

    public static void Run()
    {
        Stores.Clear();

        IComponentStore positions = StoreFor(typeof(Vector2));

        positions.AddBoxed(new Vector2(1f, 2f));
        positions.AddBoxed(new Vector2(3f, 4f));

        Check.Equal(positions.Count, 2, "MakeGenericType ferme un type OUVERT sur un type connu seulement a l'execution");

        Check.True(positions is ComponentStore<Vector2>,
            "et ce qui en sort est un VRAI ComponentStore<Vector2>, pas une version affaiblie : la liste a l'interieur est une List<Vector2>, sans emballage");

        Check.Equal(((ComponentStore<Vector2>)positions)[0], new Vector2(1f, 2f), "on peut donc le manipuler de facon typee des qu'on connait le type");

        IComponentStore healths = StoreFor(typeof(int));

        healths.AddBoxed(50);

        Check.Equal(healths.Count, 1, "un autre type ferme donne un autre magasin");
        Check.Equal(positions.Count, 2, "et les deux ne se melangent pas");

        Check.True(ReferenceEquals(StoreFor(typeof(int)), healths),
            "le dictionnaire sert de cache : MakeGenericType et Activator sont chers, on ne les appelle qu'une fois par type");

        Check.Equal(Stores.Count, 2, "deux types vus, deux magasins");

        Check.Equal(CallGeneric(nameof(Describe), typeof(int), new object[] { 42 }), "Int32:42",
            "MakeGenericMethod fait la meme chose pour une METHODE : on decouvre T a l'execution et on appelle quand meme la version generique");

        Check.Equal(CallGeneric(nameof(Describe), typeof(string), new object[] { "epee" }), "String:epee",
            "avec un autre T, sans une ligne de plus");

        Check.Equal(Describe(1.5f), "Single:1.5",
            "alors qu'appelee normalement, la meme methode ne coute rien : c'est le CHEMIN par reflexion qui est cher, pas la genericite");

        Check.Throws<ArgumentException>(() => typeof(ComponentStore<>).MakeGenericType(typeof(int), typeof(int)),
            "fermer avec le mauvais nombre d'arguments echoue a l'execution : la reflexion deplace toutes les erreurs de la compilation vers l'execution, et c'est son cout reel");

        Check.True(typeof(ComponentStore<>).IsGenericTypeDefinition, "le type ouvert est un moule");
        Check.False(typeof(ComponentStore<int>).IsGenericTypeDefinition, "le ferme est un vrai type");

        Check.Equal(typeof(ComponentStore<int>).GetGenericTypeDefinition(), typeof(ComponentStore<>),
            "et on passe de l'un a l'autre dans les deux sens, exactement comme dans generic2 - sauf qu'ici c'est nous qui fabriquons le type au lieu d'en reconnaitre un");

        Check.True(Stores.Values.All(store => store.Count > 0),
            "dernier avertissement : sur IL2CPP, une combinaison generique que le compilateur n'a jamais VUE n'existe pas. ComponentStore<MonType> fabrique par reflexion echoue sur console si rien dans le code ne l'instancie explicitement");
    }
}
