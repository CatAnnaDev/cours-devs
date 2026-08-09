namespace Csharplings;

public static class Flow3
{
    public const bool NotDone = true;

    public static int SumTo(int max)
    {
        int total = 0;

        for (int i = 0; i < max; i++)
            total += i;

        return total;
    }

    public static int HitsToKill(int health, int damagePerHit)
    {
        int hits = 0;

        while (health > 0)
        {
            health -= damagePerHit;
            hits++;
        }

        return hits;
    }

    public static void Run()
    {
        Check.Equal(SumTo(10), 55, "la somme de 1 a 10 (attention au dernier tour)");
        Check.Equal(SumTo(1), 1, "la somme de 1 a 1");
        Check.Equal(SumTo(0), 0, "aucun tour de boucle");

        Check.Equal(HitsToKill(100, 30), 4, "100 PV, 30 degats par coup");
        Check.Equal(HitsToKill(10, 10), 1, "un seul coup suffit");
    }
}
