namespace Csharplings;

public sealed record Pickup(string Name, Vector2 Position);

public static class Empty1
{
    public const bool NotDone = false;

    private static readonly List<Pickup> Pickups = new()
    {
        new Pickup("cle", new Vector2(50f, 0f)),
        new Pickup("potion", new Vector2(10f, 0f)),
    };

    public static Pickup NearestOrNull(Vector2 from, float range) =>
        Pickups
            .Where(pickup => pickup.Position.DistanceTo(from) <= range)
            .MinBy(pickup => pickup.Position.DistanceTo(from));

    public static bool TryNearest(Vector2 from, float range, out Pickup found)
    {
        found = NearestOrNull(from, range);

        return found is not null;
    }

    public static Vector2 FirstPositionOr(Vector2 fallback) =>
        Pickups.Select(pickup => pickup.Position).DefaultIfEmpty(fallback).First();

    public static void Run()
    {
        Check.Equal(NearestOrNull(Vector2.Zero, 100f).Name, "potion",
            "le PLUS PROCHE, pas le premier de la liste : la cle vient avant dans la source et se trouve cinq fois plus loin");
        Check.True(NearestOrNull(Vector2.Zero, 1f) is null, "et null quand rien n'est assez proche");

        Check.True(TryNearest(Vector2.Zero, 100f, out Pickup near) && near.Name == "potion",
            "la version en 'try' rend un bool : l'appelant ne peut pas oublier de tester");

        Check.False(TryNearest(Vector2.Zero, 1f, out _), "et false quand il n'y a rien");

        Check.Equal(Pickups.Where(pickup => pickup.Name == "absent").FirstOrDefault(), null,
            "FirstOrDefault sur des objets rend null : la reponse est claire");

        Check.Equal(Pickups.Where(pickup => pickup.Name == "absent").Select(pickup => pickup.Position).FirstOrDefault(),
            Vector2.Zero,
            "sur un STRUCT, il rend default(T), donc Vector2.Zero. Rien ne distingue 'pas trouve' de 'trouve a l'origine', et l'ennemi fonce vers le coin de la carte");

        Check.Equal(Pickups.Select(pickup => pickup.Position).FirstOrDefault(position => position.X > 999f, new Vector2(-1f, -1f)),
            new Vector2(-1f, -1f),
            "d'ou la surcharge qui prend une valeur de repli EXPLICITE : impossible de la confondre avec une vraie position");

        Check.Near(FirstPositionOr(new Vector2(-1f, -1f)), new Vector2(50f, 0f), "DefaultIfEmpty ne change rien quand la sequence a des elements");

        Check.Throws<InvalidOperationException>(() => Pickups.First(pickup => pickup.Name == "absent"),
            "First sans repli LEVE quand il ne trouve pas : c'est le bon choix quand l'absence est un bug, pas un cas de jeu");

        Check.Equal(Pickups.Single(pickup => pickup.Name == "cle").Name, "cle", "Single exige exactement un resultat");

        Check.Throws<InvalidOperationException>(() => Pickups.Single(),
            "et leve s'il y en a plusieurs : c'est la facon la plus courte d'affirmer 'il ne peut y en avoir qu'un', par exemple pour un singleton de scene");

        Check.True(Pickups.SingleOrDefault(pickup => pickup.Name == "absent") is null,
            "SingleOrDefault tolere zero resultat");

        Check.Throws<InvalidOperationException>(() => Pickups.SingleOrDefault(),
            "mais toujours pas deux : le 'OrDefault' porte sur le vide, jamais sur le trop-plein");

        Check.Equal(Array.Empty<int>().Sum(), 0, "la somme de rien vaut zero, ce qui est une reponse");

        Check.Throws<InvalidOperationException>(() => Array.Empty<int>().Max(),
            "le maximum de rien n'en est pas une, et LINQ refuse d'inventer");

        Check.Equal(Array.Empty<int>().DefaultIfEmpty(-1).Max(), -1, "sauf si on dit quoi mettre a la place");

        Check.True(Array.Empty<Pickup>().MaxBy(pickup => pickup.Position.X) is null,
            "et MaxBy rend null sur une sequence vide d'objets, parce que lui rend un ELEMENT et que l'absence d'element se represente");
    }
}
