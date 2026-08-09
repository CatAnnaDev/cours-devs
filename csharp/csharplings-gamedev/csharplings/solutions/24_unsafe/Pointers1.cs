namespace Csharplings;

public static unsafe class Pointers1
{
    public const bool NotDone = false;

    public static int SumThroughPointer(int[] values)
    {
        int total = 0;

        fixed (int* start = values)
        {
            int* end = start + values.Length;

            for (int* cursor = start; cursor < end; cursor++)
                total += *cursor;
        }

        return total;
    }

    public static void ScaleThroughPointer(float[] values, float factor)
    {
        fixed (float* start = values)
        {
            for (int i = 0; i < values.Length; i++)
                start[i] *= factor;
        }
    }

    public static int DistanceInElements(int[] values, int fromIndex, int toIndex)
    {
        fixed (int* start = values)
            return (int)(&start[toIndex] - &start[fromIndex]);
    }

    public static int ByteStride() => sizeof(Vector2);

    public static char FirstLetter(string text)
    {
        fixed (char* start = text)
            return *start;
    }

    public static void Run()
    {
        var values = new[] { 1, 2, 3, 4 };

        Check.Equal(SumThroughPointer(values), 10,
            "un pointeur pointe le PREMIER element du tableau, et l'incrementer avance d'un element, pas d'un octet");

        Check.Equal(DistanceInElements(values, 0, 3), 3,
            "soustraire deux pointeurs rend un nombre d'ELEMENTS : le compilateur divise par la taille du type pour toi");

        Check.Equal(sizeof(int), 4, "un int pese quatre octets");
        Check.Equal(sizeof(float), 4, "un float aussi");
        Check.Equal(sizeof(double), 8, "un double le double");
        Check.Equal(ByteStride(), 8, "et un Vector2, deux floats, en pese huit : c'est le pas dont avance un Vector2*");

        var speeds = new[] { 1f, 2f, 3f };

        ScaleThroughPointer(speeds, 2f);

        Check.Sequence(speeds, new[] { 2f, 4f, 6f },
            "ecrire a travers un pointeur ecrit dans le VRAI tableau : il n'y a pas de copie, il n'y a que l'adresse");

        Check.Equal(FirstLetter("gobelin"), 'g',
            "une string s'epingle comme un tableau : ses caracteres sont contigus en memoire, et c'est comme ca qu'on la passe a une API native");

        Check.Equal(values[0], 1, "le tableau d'origine est intact");

        int[] moved = new int[4];

        fixed (int* pinned = moved)
        {
            GC.Collect();

            *pinned = 42;
        }

        Check.Equal(moved[0], 42,
            "voila pourquoi 'fixed' existe : le ramasse-miettes DEPLACE les objets pour compacter le tas, et une adresse notee avant un deplacement ne designerait plus rien. 'fixed' epingle l'objet le temps du bloc");

        Check.True(sizeof(Vector2) * 100 == 800,
            "et c'est ce qui rend un tableau de structs interessant : cent Vector2, c'est huit cents octets d'un seul tenant, pas cent objets eparpilles avec leur en-tete chacun");
    }
}
