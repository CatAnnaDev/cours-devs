using Csharplings.Unity;

namespace Csharplings;

public static class Transform1
{
    public const bool NotDone = false;

    public static void MoveBadly(Transform transform, Vector2 step)
    {
        transform.Position += step;
    }

    public static void MoveOnce(Transform transform, Vector2 step)
    {
        Vector2 position = transform.Position;

        transform.Position = position + step;
    }

    public static void PlaceAndFace(Transform transform, Vector2 position, float rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
    }

    public static void MoveMany(List<Transform> transforms, Vector2 step)
    {
        for (int i = 0; i < transforms.Count; i++)
        {
            Vector2 position = transforms[i].Position;

            transforms[i].Position = position + step;
        }
    }

    public static void Run()
    {
        var gameObject = new GameObject("joueur");
        Transform transform = gameObject.AddComponent(new Transform());

        Transform.ResetCounter();

        MoveBadly(transform, new Vector2(1f, 0f));

        Check.Equal(Transform.Crossings, 2,
            "'transform.position += v' fait DEUX franchissements de la frontiere : une lecture puis une ecriture. La propriete n'est pas un champ, c'est un appel dans le moteur natif");

        Transform.ResetCounter();
        MoveOnce(transform, new Vector2(1f, 0f));

        Check.Equal(Transform.Crossings, 2,
            "l'ecrire en deux temps ne change rien : c'est toujours un aller et un retour, et c'est incompressible quand on a besoin de l'ancienne valeur");

        Transform.ResetCounter();
        PlaceAndFace(transform, new Vector2(5f, 5f), 1.5f);

        Check.Equal(Transform.Crossings, 1,
            "SetPositionAndRotation en fait UN SEUL, la ou position puis rotation en feraient deux. Sur mille objets, c'est mille traversees economisees par image");

        Check.Near(transform.Position, new Vector2(5f, 5f), "avec le meme resultat");
        Check.Equal(transform.Rotation, 1.5f, "les deux valeurs posees");

        Vector2 copy = transform.Position;

        copy += new Vector2(100f, 100f);

        Check.Near(transform.Position, new Vector2(5f, 5f),
            "et l'autre moitie du piege : la propriete rend une COPIE. La modifier ne modifie rien, et c'est pour ca que 'transform.position.x = 5' ne compile pas - le compilateur refuse plutot que de te laisser y croire");

        transform.Position = new Vector2(copy.X, transform.Position.Y);

        Check.Near(transform.Position, new Vector2(105f, 5f),
            "changer une seule composante demande donc de reconstruire le vecteur entier et de le REECRIRE");

        var many = new List<Transform>();

        for (int i = 0; i < 100; i++)
            many.Add(new GameObject("ennemi" + i).AddComponent(new Transform()));

        Transform.ResetCounter();
        MoveMany(many, new Vector2(0f, 1f));

        Check.Equal(Transform.Crossings, 200,
            "cent objets deplaces, deux cents traversees. C'est le meme constat que 18_bridge cote Godot : la frontiere se paye a chaque propriete, pas a chaque objet");

        Transform.ResetCounter();

        for (int i = 0; i < many.Count; i++)
            many[i].SetPositionAndRotation(new Vector2(i, 0f), 0f);

        Check.Equal(Transform.Crossings, 100, "la version qui pose tout d'un coup en fait moitie moins");

        Transform.ResetCounter();

        Vector2 total = Vector2.Zero;

        for (int i = 0; i < many.Count; i++)
            total += many[i].Position;

        Check.Equal(Transform.Crossings, 100, "et LIRE coute aussi : une lecture par objet");

        Check.Near(total, new Vector2(4950f, 0f), "la somme des positions");

        Check.True(Transform.Crossings > 0,
            "la parade est toujours la meme : garder les positions dans un tableau a soi, calculer dessus, et n'ecrire dans le moteur qu'une fois par objet et par image");
    }
}
