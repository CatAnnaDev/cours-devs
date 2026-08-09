using Csharplings.Unity;

namespace Csharplings;

public sealed class Grunt : MonoBehaviour
{
    public int UpdatesSeen { get; private set; }

    public int DestroyCalls { get; private set; }

    public override void Update() => UpdatesSeen++;

    public override void OnDestroy() => DestroyCalls++;
}

public static class Destroyed1
{
    public const bool NotDone = true;

    public static bool IsMissing(UnityObject target) => target is null;

    public static bool IsNullReference(UnityObject target) => target == null;

    public static void Run()
    {
        var scene = new Scene();
        Grunt grunt = scene.Add(new Grunt { Name = "Gobelin" });

        scene.Frame();

        Check.Equal(grunt.UpdatesSeen, 1, "une frame, un Update");
        Check.False(IsMissing(grunt), "un objet vivant n'est pas manquant");
        Check.False(IsNullReference(grunt), "et sa reference n'est pas nulle");

        UnityObject.DestroyImmediate(grunt);

        Check.Equal(UnityObject.PendingDestructionCount, 1,
            "Destroy ne detruit pas tout de suite : il met l'objet dans la file de FIN DE FRAME");
        Check.True(grunt.NativeAlive, "l'objet natif vit donc encore pendant tout le reste de la frame");
        Check.False(IsMissing(grunt), "et '== null' rend encore false");

        scene.Frame();

        Check.Equal(grunt.UpdatesSeen, 2,
            "un objet condamne recoit encore son Update jusqu'a la fin de la frame : c'est voulu, mais il faut le savoir");
        Check.Equal(grunt.DestroyCalls, 1, "puis OnDestroy passe, une seule fois");
        Check.False(grunt.NativeAlive, "et l'objet natif a disparu");

        Check.True(IsMissing(grunt),
            "MAINTENANT 'grunt == null' rend TRUE, alors que la variable contient toujours une reference bien vivante. Unity surcharge l'operateur");
        Check.False(IsNullReference(grunt),
            "'grunt is null' rend false : un motif ne passe PAS par l'operateur. Les deux ecritures ne veulent pas dire la meme chose");
        Check.True(ReferenceEquals(grunt, grunt),
            "l'objet C# est toujours la : c'est le natif, derriere, qui a ete libere. Deux objets pour une variable, comme chez Godot");

        Check.Throws<InvalidOperationException>(() => grunt.Describe(),
            "toucher a l'objet detruit leve une erreur : c'est le MissingReferenceException de Unity");

        Check.Throws<InvalidOperationException>(() => grunt?.Describe(),
            "et le '?.' ne protege de RIEN ici : il teste la reference, qui n'est pas nulle. Piege classique, personne ne le voit venir");

        UnityObject sameSlot = grunt;

        Check.True(sameSlot == null, "n'importe quelle variable qui pointe l'objet detruit se comporte pareil");
        Check.True(grunt.Equals(null),
            "Equals suit l'operateur, lui : c'est coherent, mais ca surprend la premiere fois");

        var scene2 = new Scene();
        Grunt immediate = scene2.Add(new Grunt { Name = "Rat" });

        UnityObject.DestroyImmediate(immediate);

        Check.False(immediate.NativeAlive, "DestroyImmediate, lui, detruit sur place");
        Check.Equal(UnityObject.PendingDestructionCount, 0, "sans passer par la file");
        Check.Equal(immediate.UpdatesSeen, 0, "l'objet ne verra donc aucun Update de cette frame");

        scene2.Frame();

        Check.Equal(immediate.UpdatesSeen, 0, "ni de la suivante");
        Check.Equal(immediate.DestroyCalls, 1, "et son OnDestroy passe a la fin de la frame en cours");

        var survivor = new Scene();
        Grunt kept = survivor.Add(new Grunt { Name = "Chef" });

        survivor.Frames(5);

        Check.Equal(kept.UpdatesSeen, 5, "un objet qu'on ne detruit pas continue simplement de vivre");
        Check.Equal(kept.DestroyCalls, 0, "et son OnDestroy ne part jamais tout seul");
        Check.False(IsMissing(kept), "la regle a retenir : sur un objet Unity on teste '== null', jamais 'is null'");
    }
}
