using Csharplings.Unity;

namespace Csharplings;

public static class DamageMath
{
    public static float ApplyArmor(float raw, float armor) => raw * (100f / (100f + (armor < 0f ? 0f : armor)));
}

public sealed class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public int Played { get; private set; }

    public override void Awake()
    {
        Instance = this;
    }

    public override void OnDestroy()
    {
        Instance = null;
    }

    public void Play() => Played++;
}

public sealed class LevelProp : MonoBehaviour
{
}

public static class Singleton1
{
    public const bool NotDone = true;

    public static void Run()
    {
        var scene = new Scene();
        AudioManager manager = scene.Add(new AudioManager { Name = "Audio" });

        Check.True(ReferenceEquals(AudioManager.Instance, manager),
            "le premier a entrer dans la scene devient l'instance unique, et il le fait dans Awake pour etre pret avant tous les Start");

        manager.Play();
        manager.Play();

        Check.Equal(AudioManager.Instance.Played, 2, "l'ETAT vit dans l'instance");

        Check.Near(DamageMath.ApplyArmor(100f, 100f), 50.0,
            "alors que le COMPORTEMENT pur va en static : pas d'instance, pas de singleton, juste une fonction", 0.01);
        Check.Near(DamageMath.ApplyArmor(100f, 0f), 100.0, "et elle est testable sans rien construire", 0.01);

        AudioManager duplicate = scene.Add(new AudioManager { Name = "AudioBis" });

        Check.True(ReferenceEquals(AudioManager.Instance, manager),
            "un second exemplaire ne prend PAS la place : il se detruit lui-meme, c'est le cas d'une scene chargee deux fois");
        Check.Equal(UnityObject.PendingDestructionCount, 1, "sa destruction est en attente de fin de frame");

        scene.Frame();

        Check.True(duplicate == null, "le doublon est parti");
        Check.False(manager == null, "l'original est intact");
        Check.True(ReferenceEquals(AudioManager.Instance, manager),
            "et l'instance pointe toujours sur lui : le OnDestroy du doublon ne l'a pas mise a null, parce qu'il verifie 'si c'est encore moi'");
        Check.Equal(AudioManager.Instance.Played, 2, "avec son etat intact");

        scene.Add(new LevelProp { Name = "Caisse" });

        Check.Equal(scene.BehaviourCount, 2, "la scene contient le gestionnaire et un decor");

        scene.Unload();

        Check.Equal(scene.BehaviourCount, 1,
            "au changement de scene, le decor disparait mais le gestionnaire survit : c'est DontDestroyOnLoad, pose une seule fois dans Awake");
        Check.True(ReferenceEquals(AudioManager.Instance, manager), "et l'instance tient toujours");
        Check.Equal(AudioManager.Instance.Played, 2, "avec son etat : c'est tout l'interet, la musique ne repart pas de zero");

        UnityObject.Destroy(manager);
        scene.Frame();

        Check.True(AudioManager.Instance is null,
            "detruit proprement par la scene, son OnDestroy remet l'instance a null : la reference est vraiment nulle");

        var reborn = new Scene();
        AudioManager second = reborn.Add(new AudioManager { Name = "Audio2" });

        Check.True(ReferenceEquals(AudioManager.Instance, second), "un nouveau peut donc prendre la place");
        Check.Equal(AudioManager.Instance.Played, 0, "et il repart d'un etat neuf");

        UnityObject.DestroyImmediate(second);

        Check.True(AudioManager.Instance == null,
            "mais si l'objet meurt sans que OnDestroy passe, la propriete statique garde une reference PERIMEE. '== null' rend true, donc le garde de Awake fonctionne");
        Check.False(AudioManager.Instance is null,
            "alors que 'is null' rend false : la reference est bien la, c'est l'objet natif qui est mort. Ecrire le garde en 'is null' laisserait donc le singleton casse pour toute la partie");
    }
}
