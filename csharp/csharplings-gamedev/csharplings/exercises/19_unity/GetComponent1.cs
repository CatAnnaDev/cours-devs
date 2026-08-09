using Csharplings.Unity;

namespace Csharplings;

public sealed class Vitals : Component
{
    public int Current { get; set; } = 100;
}

public sealed class Barrier : Component
{
    public int Charges { get; set; } = 2;
}

public sealed class NaiveDamage : MonoBehaviour
{
    public override void Update() => GetComponent<Vitals>().Current -= 1;
}

public sealed class CachedDamage : MonoBehaviour
{
    public override void Update() => GetComponent<Vitals>().Current -= 1;
}

public static class GetComponent1
{
    public const bool NotDone = true;

    public static void Run()
    {
        var naiveTarget = new GameObject("Naif");
        Vitals naiveVitals = naiveTarget.AddComponent(new Vitals());
        var naiveScene = new Scene();

        GameObject.LookupCount = 0;
        naiveScene.Add(naiveTarget.AddComponent(new NaiveDamage()));
        naiveScene.Frames(60);

        int naiveLookups = Report("GetComponent appele dans Update, 60 frames", GameObject.LookupCount);

        Check.Equal(naiveLookups, 60,
            "soixante recherches pour soixante frames. Chaque GetComponent parcourt les composants de l'objet, et c'est un appel natif");
        Check.Equal(naiveVitals.Current, 40, "les degats sont bien passes : soixante frames, soixante points");

        var cachedTarget = new GameObject("Cache");
        Vitals cachedVitals = cachedTarget.AddComponent(new Vitals());
        var cachedScene = new Scene();

        GameObject.LookupCount = 0;
        cachedScene.Add(cachedTarget.AddComponent(new CachedDamage()));
        cachedScene.Frames(60);

        int cachedLookups = Report("le meme travail avec une recherche dans Awake", GameObject.LookupCount);

        Check.Equal(cachedLookups, 1,
            "UNE seule recherche, au demarrage, puis plus jamais : on cherche une fois et on garde");
        Check.Equal(cachedVitals.Current, 40, "et le resultat est identique au point pres");
        Check.True(naiveLookups > cachedLookups, "soixante contre une, pour exactement le meme jeu");

        Check.True(cachedTarget.TryGetComponent(out Vitals found),
            "TryGetComponent rend true quand le composant est la");
        Check.Equal(found.Current, 40, "et remplit la variable");

        Check.False(cachedTarget.TryGetComponent(out Barrier absent),
            "et false quand il n'y est pas, sans rien allouer ni rien logguer");
        Check.True(absent is null, "la variable reste nulle : c'est ce qui remplace le GetComponent suivi d'un test");

        UnityObject.ComparisonCount = 0;
        TestInsideTheLoop(cachedVitals, 1000);

        int inside = Report("1000 tours avec le test != null DANS la boucle", UnityObject.ComparisonCount);

        UnityObject.ComparisonCount = 0;
        TestOutsideTheLoop(cachedVitals, 1000);

        int outside = Report("le meme avec le test sorti de la boucle", UnityObject.ComparisonCount);

        Check.Equal(inside, 1000,
            "l'operateur == de Unity n'est pas un test de reference : il demande au moteur si l'objet natif vit encore. Mille tours, mille questions");
        Check.Equal(outside, 1,
            "sorti de la boucle, une seule. Dans une boucle chaude, on teste une fois et on garde la reponse");

        var doomed = new GameObject("Condamne");
        Vitals doomedVitals = doomed.AddComponent(new Vitals());

        Check.False(doomedVitals == null, "le composant est vivant");

        UnityObject.DestroyImmediate(doomed);

        Check.True(doomedVitals == null,
            "detruire l'OBJET detruit aussi ses composants : une reference cachee devient invalide sans que personne te previenne");
        Check.False(doomedVitals is null,
            "et comme toujours chez Unity, 'is null' ne le voit pas : la reference existe encore");

        var fragile = new GameObject("Fragile");
        Vitals fragileVitals = fragile.AddComponent(new Vitals());
        var fragileScene = new Scene();

        fragileScene.Add(fragile.AddComponent(new CachedDamage()));
        fragileScene.Frames(5);

        Check.Equal(fragileVitals.Current, 95, "cinq frames, cinq degats");

        UnityObject.DestroyImmediate(fragileVitals);
        fragileScene.Frames(5);

        Check.Equal(fragileVitals.Current, 95,
            "la cible detruite, le test != null protege le reste : plus de degats, et surtout aucun crash. C'est pour ca qu'une reference cachee se verifie, pas juste se garde");
    }

    private static void TestInsideTheLoop(Vitals vitals, int rounds)
    {
        for (int i = 0; i < rounds; i++)
        {
            if (vitals != null)
                vitals.Current = i;
        }
    }

    private static void TestOutsideTheLoop(Vitals vitals, int rounds)
    {
        for (int i = 0; i < rounds; i++)
        {
            if (vitals != null)
                vitals.Current = i;
        }
    }

    private static int Report(string what, int count)
    {
        Console.WriteLine($"      mesure  {what} : {count}");

        return count;
    }
}
