using Csharplings.Unity;

namespace Csharplings;

public static class Material1
{
    public const bool NotDone = true;

    public static void TintShared(MeshRenderer renderer, float alpha) => renderer.Material.Alpha = alpha;

    public static void TintOne(MeshRenderer renderer, float alpha) => renderer.Material.Alpha = alpha;

    public static void Run()
    {
        Material.ResetCounter();

        var shared = new Material("standard") { Name = "Rouge" };
        var first = new MeshRenderer(shared);
        var second = new MeshRenderer(shared);

        Check.Equal(Material.Created, 1, "un seul materiau en memoire, partage par les deux afficheurs");
        Check.False(first.HasOwnInstance, "aucun des deux n'a sa copie");
        Check.False(second.HasOwnInstance, "pour l'instant");

        TintShared(first, 0.5f);

        Check.Equal(Material.Created, 1, "toucher a SharedMaterial ne cree rien");
        Check.Near(second.Rendered.Alpha, 0.5, "mais ca change les DEUX : c'est le meme objet, et c'est parfois exactement ce qu'on veut");

        TintOne(first, 0.1f);

        Check.Equal(Material.Created, 2,
            "lire '.material' CLONE le materiau. Une propriete, pas une methode, et elle alloue : c'est le piege le plus couteux de Unity");
        Check.True(first.HasOwnInstance, "le premier afficheur a maintenant sa copie personnelle");
        Check.False(second.HasOwnInstance, "le second, non");

        Check.Near(first.Rendered.Alpha, 0.1, "le premier affiche sa version");
        Check.Near(second.Rendered.Alpha, 0.5, "le second continue d'afficher le partage");
        Check.Near(shared.Alpha, 0.5, "et le materiau partage n'a pas ete abime");

        Material.ResetCounter();

        var crowd = new List<MeshRenderer>(100);

        for (int i = 0; i < 100; i++)
            crowd.Add(new MeshRenderer(shared));

        Check.Equal(Material.Created, 0, "cent afficheurs sur un materiau partage : zero materiau de plus");

        foreach (MeshRenderer renderer in crowd)
            TintOne(renderer, 0.9f);

        int clones = Report("materiaux crees par 100 lectures de '.material'", Material.Created);

        Check.Equal(clones, 100,
            "cent lectures de '.material', cent materiaux. Et ils ne sont PAS ramasses par le ramasse-miettes : ce sont des objets natifs, il faut les detruire a la main");

        int alive = 0;

        foreach (MeshRenderer renderer in crowd)
        {
            if (renderer.Material.NativeAlive)
                alive++;
        }

        Check.Equal(alive, 100, "ils sont tous encore la");

        foreach (MeshRenderer renderer in crowd)
            UnityObject.Destroy(renderer.Material);

        int leaked = 0;

        foreach (MeshRenderer renderer in crowd)
        {
            if (renderer.Material.NativeAlive)
                leaked++;
        }

        Check.Equal(leaked, 0,
            "il faut donc un Destroy pour chaque clone, dans OnDestroy. Sans ca la memoire monte a chaque spawn et ne redescend jamais");

        Material.ResetCounter();

        var careful = new MeshRenderer(shared);

        Check.Near(careful.Rendered.Alpha, 0.5, "lire ce qui est AFFICHE ne clone pas");
        Check.Equal(Material.Created, 0, "donc pour juste consulter une couleur, on passe par le partage");
        Check.False(careful.HasOwnInstance, "et l'afficheur reste sur le materiau commun");
    }

    private static int Report(string what, int count)
    {
        Console.WriteLine($"      mesure  {what} : {count} materiaux");

        return count;
    }
}
