using Csharplings.Unity;

namespace Csharplings;

public static class Journal
{
    private static readonly List<string> Lines = new();

    public static void Write(string line) => Lines.Add(line);

    public static string Read() => string.Join(" ", Lines);

    public static void Clear() => Lines.Clear();
}

public sealed class Actor : MonoBehaviour
{
    public Actor(string label)
    {
        Label = label;

        Journal.Write($"{label}.ctor");
    }

    public string Label { get; }

    public int Updates { get; private set; }

    public override void Awake() => Journal.Write($"{Label}.Awake");

    public override void OnEnable() => Journal.Write($"{Label}.OnEnable");

    public override void Start() => Journal.Write($"{Label}.Start");

    public override void Update()
    {
        Updates++;

        Journal.Write($"{Label}.Update");
    }

    public override void OnDisable() => Journal.Write($"{Label}.OnDisable");

    public override void OnDestroy() => Journal.Write($"{Label}.OnDestroy");
}

public sealed class Bell
{
    public event Action Rung;

    public int Listeners => Rung?.GetInvocationList().Length ?? 0;

    public void Ring() => Rung?.Invoke();
}

public sealed class Listener : MonoBehaviour
{
    private readonly Bell _bell;

    public Listener(Bell bell)
    {
        _bell = bell;
    }

    public int Heard { get; private set; }

    public override void OnEnable() => _bell.Rung += OnRung;

    public override void OnDisable() => _bell.Rung -= OnRung;

    private void OnRung() => Heard++;
}

public static class Lifecycle1
{
    public const bool NotDone = false;

    public static void Run()
    {
        Subscriptions();
        Journal.Clear();

        var scene = new Scene();
        var first = new Actor("A");
        var second = new Actor("B");

        Check.Equal(Journal.Read(), "A.ctor B.ctor",
            "le constructeur tourne AVANT que l'objet soit dans la scene : aucune API du moteur n'y est utilisable");

        Journal.Clear();
        scene.Add(first);
        scene.Add(second);

        Check.Equal(Journal.Read(), "A.Awake A.OnEnable B.Awake B.OnEnable",
            "a l'entree dans la scene : Awake puis OnEnable, objet par objet. Toujours dans cet ordre");

        Journal.Clear();
        scene.Frame();

        Check.Equal(Journal.Read(), "A.Start B.Start A.Update B.Update",
            "premiere frame : TOUS les Start passent avant TOUS les Update. C'est pour ca que chercher un autre objet dans Awake est un pari, et dans Start une certitude");

        Journal.Clear();
        scene.Frame();

        Check.Equal(Journal.Read(), "A.Update B.Update",
            "les frames suivantes n'ont plus que les Update : Start ne repasse jamais");

        Journal.Clear();
        first.SetEnabled(false);

        Check.Equal(Journal.Read(), "A.OnDisable", "desactiver declenche OnDisable, tout de suite");

        Journal.Clear();
        scene.Frame();

        Check.Equal(Journal.Read(), "B.Update", "et un script desactive ne recoit plus rien du tout");
        Check.Equal(first.Updates, 2, "son compteur est reste ou il etait");

        Journal.Clear();
        first.SetEnabled(true);

        Check.Equal(Journal.Read(), "A.OnEnable",
            "reactiver rejoue OnEnable, et SEULEMENT lui : ni Awake ni Start ne repassent. C'est pour ca qu'on s'abonne dans OnEnable et qu'on se desabonne dans OnDisable, pas dans Start et OnDestroy");

        Journal.Clear();
        scene.Frame();

        Check.Equal(Journal.Read(), "A.Update B.Update", "et il reprend sa place dans la boucle");

        Journal.Clear();
        UnityObject.Destroy(first);

        Check.Equal(Journal.Read(), string.Empty,
            "Destroy ne declenche rien tout de suite : l'objet vit jusqu'a la fin de la frame");

        scene.Frame();

        Check.Equal(Journal.Read(), "A.Update B.Update A.OnDisable A.OnDestroy",
            "il recoit encore son Update, PUIS OnDisable, PUIS OnDestroy. La destruction passe par OnDisable : un desabonnement ecrit la est donc fait dans les deux cas");

        Journal.Clear();
        scene.Frame();

        Check.Equal(Journal.Read(), "B.Update", "et il a bien disparu de la boucle");

        Journal.Clear();

        var late = new Actor("C");

        scene.Add(late);

        Check.Equal(Journal.Read(), "C.ctor C.Awake C.OnEnable",
            "un script ajoute en pleine partie recoit son Awake immediatement, pas a la frame suivante");

        Journal.Clear();
        scene.Frame();

        Check.Equal(Journal.Read(), "C.Start B.Update C.Update",
            "et son Start passe avant son premier Update, comme pour les autres");
    }

    private static void Subscriptions()
    {
        var bell = new Bell();
        var scene = new Scene();
        Listener listener = scene.Add(new Listener(bell));

        scene.Frame();

        Check.Equal(bell.Listeners, 1, "le script est abonne");

        bell.Ring();

        Check.Equal(listener.Heard, 1, "et il entend");

        listener.SetEnabled(false);

        Check.Equal(bell.Listeners, 0,
            "desactive, il doit etre DESABONNE : sinon un objet endormi continue de reagir, et il reste en vie a cause de l'abonnement lui-meme");

        bell.Ring();

        Check.Equal(listener.Heard, 1, "il n'entend plus rien");

        listener.SetEnabled(true);

        Check.Equal(bell.Listeners, 1,
            "reactive, il se reabonne. UNE fois, pas deux : c'est pour ca que l'abonnement va dans OnEnable et pas dans Awake ni Start");

        bell.Ring();

        Check.Equal(listener.Heard, 2, "et il entend a nouveau");

        UnityObject.Destroy(listener);
        scene.Frame();

        Check.Equal(bell.Listeners, 0,
            "detruit, il est desabonne aussi : la destruction passe par OnDisable, donc un seul endroit a ecrire couvre les deux cas");

        bell.Ring();

        Check.Equal(listener.Heard, 2, "et plus rien ne le reveille");
    }
}
