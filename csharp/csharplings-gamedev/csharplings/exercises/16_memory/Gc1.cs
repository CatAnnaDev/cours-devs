namespace Csharplings;

public static class Gc1
{
    public const bool NotDone = true;

    private static object _keepsTheBoxAlive;

    public static long Bytes(Action action)
    {
        action();
        action();
        action();

        long before = GC.GetAllocatedBytesForCurrentThread();
        action();

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    public static int Garbage(int rounds)
    {
        int collectionsBefore = GC.CollectionCount(0);

        for (int i = 0; i < rounds; i++)
        {
            var junk = new byte[64];
            junk[0] = (byte)i;
        }

        return GC.CollectionCount(0) - collectionsBefore;
    }

    public static int Reused(int rounds)
    {
        int collectionsBefore = GC.CollectionCount(0);

        for (int i = 0; i < rounds; i++)
        {
            var buffer = new byte[64];
            buffer[0] = (byte)i;
        }

        return GC.CollectionCount(0) - collectionsBefore;
    }

    public static void Run()
    {
        Check.Equal(Bytes(() => { var empty = new EmptyThing(); }), 24L,
            "un objet VIDE coute quand meme 24 octets : l'en-tete que tout objet du tas porte");

        Check.Equal(Bytes(() => { var one = new OneField(); }), 24L,
            "y ajouter un int est gratuit : il tient dans le remplissage de l'en-tete");

        Check.Equal(Bytes(() => { var four = new FourFields(); }), 32L,
            "au quatrieme, il faut vraiment de la place en plus");

        Check.Equal(Bytes(() => { var nothing = new byte[0]; }), 24L,
            "meme un tableau VIDE coute son en-tete plus sa longueur");

        Check.Equal(Bytes(() => { var thousand = new Sample[1000]; }), 20024L,
            "1000 structures de 20 octets : UNE allocation de 20 024 octets, tout d'un bloc et contigu");

        Check.True(Bytes(() => _keepsTheBoxAlive = 42) > 0L,
            "et emballer un simple int fabrique un objet complet sur le tas, en-tete compris");

        Check.Equal(Bytes(() => { var thrownAway = (object)42; }), 0L,
            "sauf si personne ne la garde : en Release le compilateur supprime une boite dont le resultat ne sert a rien");

        int withGarbage = Garbage(200000);
        int withBuffer = Reused(200000);

        Check.True(withGarbage > 0,
            "200 000 petits objets jetables declenchent des collections gen0 : chacune est une PAUSE");
        Check.Equal(withBuffer, 0,
            "le meme travail avec un tampon reutilise : aucune collection, aucune pause, aucune frame perdue");
        Check.True(withGarbage > withBuffer, "c'est tout l'ecart entre 60 images par seconde et des micro-saccades");

        var kept = new byte[1000];

        Check.Equal(GC.GetGeneration(kept), 0, "un objet tout neuf nait en generation 0");

        GC.Collect();

        Check.True(GC.GetGeneration(kept) > 0,
            "s'il survit a une collection, il est PROMU : le ramasse-miettes le reverifiera de moins en moins souvent");
    }
}

public sealed class EmptyThing
{
}

public sealed class OneField
{
    public int A;
}

public sealed class FourFields
{
    public int A;
    public int B;
    public int C;
    public int D;
}

public struct Sample
{
    public float A;
    public float B;
    public float C;
    public float D;
    public float E;
}
