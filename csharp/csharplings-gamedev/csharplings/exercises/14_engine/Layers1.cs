namespace Csharplings;

[Flags]
public enum Layer
{
    None = 0,
    Player = 1,
    Enemy = 2,
    Bullet = 3,
    Wall = 4,
    Pickup = 5,
}

public static class Layers1
{
    public const bool NotDone = true;

    public static Layer With(Layer mask, Layer layer) => Todo.Value<Layer>();

    public static Layer Without(Layer mask, Layer layer) => Todo.Value<Layer>();

    public static bool HasAny(Layer mask, Layer layers) => (mask & layers) == layers;

    public static bool HasAll(Layer mask, Layer layers) => (mask & layers) != Layer.None;

    public static bool Detects(Layer mask, Layer otherLayer) => HasAny(mask, otherLayer);

    public static int Count(Layer mask)
    {
        return Todo.Value<int>();
    }

    public static void Run()
    {
        Layer bulletMask = Layer.Enemy | Layer.Wall;

        Check.True(Detects(bulletMask, Layer.Enemy), "une balle du joueur touche les ennemis");
        Check.True(Detects(bulletMask, Layer.Wall), "et s'arrete sur les murs");
        Check.False(Detects(bulletMask, Layer.Player), "mais elle traverse le joueur qui l'a tiree");
        Check.False(Detects(bulletMask, Layer.Pickup), "et les objets a ramasser");

        Check.True(HasAny(bulletMask, Layer.Player | Layer.Wall), "HasAny suffit qu'UN seul bit soit la");
        Check.False(HasAll(bulletMask, Layer.Player | Layer.Wall), "HasAll les veut TOUS : c'est le piege classique");
        Check.True(HasAll(bulletMask, Layer.Enemy | Layer.Wall), "ici les deux sont bien presents");

        Layer wider = With(bulletMask, Layer.Pickup);

        Check.True(Detects(wider, Layer.Pickup), "on ajoute une couche avec un OU");
        Check.True(Detects(wider, Layer.Enemy), "sans rien perdre au passage");
        Check.Equal(Count(wider), 3, "trois couches surveillees");

        Layer narrower = Without(wider, Layer.Wall);

        Check.False(Detects(narrower, Layer.Wall), "on en retire une avec un ET du complement");
        Check.True(Detects(narrower, Layer.Enemy), "les autres restent");
        Check.Equal(Count(narrower), 2, "il en reste deux");

        Check.Equal(Without(narrower, Layer.Wall), narrower, "retirer une couche absente ne change rien");
        Check.Equal(With(narrower, Layer.Enemy), narrower, "ajouter une couche deja la non plus");

        Check.Equal(Count(Layer.None), 0, "un masque vide ne surveille rien");
        Check.False(Detects(Layer.None, Layer.Player), "et ne detecte donc personne");

        Check.True(Detects(Layer.Enemy, Layer.Enemy), "un masque a une seule couche fonctionne pareil");
        Check.Equal((int)(Layer.Player | Layer.Bullet), 5, "les couches sont des bits : 1 et 4 font 5");
    }
}
