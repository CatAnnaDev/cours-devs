namespace Csharplings;

public struct Tile
{
    public int Kind;
}

public sealed class Sprite
{
    public int Frame;
}

public static class Stack1
{
    public const bool NotDone = false;

    public static void Mutate(Tile tile) => tile.Kind = 99;

    public static void Mutate(Sprite sprite) => sprite.Frame = 99;

    public static void Replace(Sprite sprite) => sprite = new Sprite { Frame = 99 };

    public static void Replace(ref Sprite sprite) => sprite = new Sprite { Frame = 99 };

    public static void Run()
    {
        var tile = new Tile { Kind = 1 };
        Tile copy = tile;
        copy.Kind = 7;

        Check.Equal(tile.Kind, 1, "affecter une structure la COPIE : deux variables, deux structures independantes");

        var sprite = new Sprite { Frame = 1 };
        Sprite alias = sprite;
        alias.Frame = 7;

        Check.Equal(sprite.Frame, 7, "affecter un objet ne copie que l'ADRESSE : deux variables, un seul objet sur le tas");

        Mutate(tile);
        Check.Equal(tile.Kind, 1, "un parametre structure est une copie : la methode a modifie la sienne, puis l'a jetee");

        Mutate(sprite);
        Check.Equal(sprite.Frame, 99, "un parametre objet pointe le meme objet : la modification se voit dehors");

        var target = new Sprite { Frame = 1 };
        Replace(target);

        Check.Equal(target.Frame, 1,
            "mais la REFERENCE est passee par valeur : reassigner le parametre ne touche pas la variable d'origine");

        Replace(ref target);

        Check.Equal(target.Frame, 99, "avec 'ref' on passe la variable elle-meme, et la reassignation sort de la methode");

        var tiles = new Tile[3];

        Check.Equal(tiles[0].Kind, 0, "un tableau de structures contient les structures : elles existent deja, a zero");

        var sprites = new Sprite[3];

        Check.Equal(sprites[0], null,
            "un tableau d'objets ne contient que des adresses, toutes nulles : aucun objet n'a encore ete cree");
        Check.Throws<NullReferenceException>(
            () =>
            {
                int frame = sprites[0].Frame;
            },
            "d'ou le NullReference du debut de chaque projet");

        object boxedFirst = 42;
        object boxedSecond = 42;

        Check.False(ReferenceEquals(boxedFirst, boxedSecond),
            "mettre un int dans un object fabrique un NOUVEL objet sur le tas a chaque fois : c'est le boxing");
        Check.True(boxedFirst.Equals(boxedSecond), "meme si les deux contiennent la meme valeur");
        Check.Equal((int)boxedFirst, 42, "et on peut ressortir la valeur en deballant");

        string first = "epee";
        string second = "epee";

        Check.True(ReferenceEquals(first, second),
            "deux litteraux identiques partagent le meme objet : le compilateur les met en commun");
    }
}
