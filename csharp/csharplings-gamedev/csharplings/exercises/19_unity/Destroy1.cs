using Csharplings.Unity;

namespace Csharplings;

public sealed class Spawner : MonoBehaviour
{
    public readonly List<GameObject> Spawned = new();

    public int Cleaned { get; private set; }

    public GameObject Spawn(string name)
    {
        var created = new GameObject(name);

        Spawned.Add(created);

        return created;
    }

    public void KillAll()
    {
        foreach (GameObject target in Spawned)
            Destroy(target);
    }

    public int Sweep()
    {
        int removed = Spawned.RemoveAll(target => target is null);

        Cleaned += removed;

        return removed;
    }
}

public static class Destroy1
{
    public const bool NotDone = true;

    public static void Run()
    {
        Time.Reset();

        var scene = new Scene();
        Spawner spawner = scene.Add(new Spawner());

        GameObject first = spawner.Spawn("gobelin");
        GameObject second = spawner.Spawn("slime");

        Check.False(first == null, "un objet neuf est bien vivant");
        Check.Equal(spawner.Spawned.Count, 2, "deux objets crees");

        UnityObject.Destroy(first);

        Check.False(first == null,
            "LE piege : juste apres Destroy, l'objet est TOUJOURS la. Unity ne detruit rien tout de suite, il inscrit l'objet sur une liste et le supprime a la fin de l'image");

        Check.Equal(UnityObject.PendingDestructionCount, 1, "il attend son tour");
        Check.Equal(spawner.Sweep(), 0,
            "donc un nettoyage lance dans la MEME image ne trouve rien a nettoyer, et l'objet mort reste dans ta liste jusqu'a la prochaine");

        Check.Equal(spawner.Spawned.Count, 2, "les deux sont encore la");

        scene.Frame();

        Check.True(first == null, "a la fin de l'image, la destruction est appliquee et l'objet devient 'null'");
        Check.Equal(UnityObject.PendingDestructionCount, 0, "la file est videe");

        Check.Equal(spawner.Sweep(), 1, "et le nettoyage de l'image suivante, lui, trouve le cadavre");
        Check.Equal(spawner.Spawned.Count, 1, "il ne reste que le slime");

        GameObject third = spawner.Spawn("rat");

        UnityObject.DestroyImmediate(third);

        Check.True(third == null,
            "DestroyImmediate, lui, detruit sur place. Il existe pour les outils d'editeur, et l'appeler pendant une partie casse tout ce qui tient encore une reference dans la meme image");

        Check.Equal(UnityObject.PendingDestructionCount, 0, "sans passer par la file");

        GameObject twice = spawner.Spawn("chauve-souris");

        UnityObject.Destroy(twice);
        UnityObject.Destroy(twice);

        Check.Equal(UnityObject.PendingDestructionCount, 1,
            "detruire deux fois le meme objet dans une image ne l'inscrit qu'une fois : sans ce garde, la file grossit et le meme objet est detruit plusieurs fois");

        scene.Frame();

        Check.True(twice == null, "et il finit detruit une seule fois");

        GameObject reused = spawner.Spawn("golem");

        UnityObject.Destroy(reused);

        Check.False(reused == null, "derniere consequence, et c'est la plus couteuse a diagnostiquer");

        reused.Name = "golem renomme";

        Check.Equal(reused.Name, "golem renomme",
            "entre le Destroy et la fin de l'image, l'objet repond encore. Tout ce que tu lui fais dans cet intervalle est perdu sans le moindre avertissement");

        scene.Frame();

        Check.True(reused == null, "puis il disparait");

        Check.Throws<InvalidOperationException>(() => reused.Describe(),
            "et l'utiliser vraiment leve enfin. La regle : apres un Destroy, on cesse d'utiliser la reference IMMEDIATEMENT - on ne compte pas sur '== null' pour nous prevenir dans la meme image");
    }
}
