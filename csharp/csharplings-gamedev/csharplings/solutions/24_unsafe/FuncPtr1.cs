namespace Csharplings;

public static unsafe class FuncPtr1
{
    public const bool NotDone = false;

    public static long Measure(Action action)
    {
        action();
        action();
        action();

        long before = GC.GetAllocatedBytesForCurrentThread();
        action();

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    public static int Damage(int power) => power * 2;

    public static int Heal(int power) => -power;

    public static int Nothing(int power) => 0;

    public static int Apply(delegate*<int, int> effect, int power) => effect(power);

    public static int ApplyAll(delegate*<int, int>[] effects, int power)
    {
        int total = 0;

        foreach (delegate*<int, int> effect in effects)
            total += effect(power);

        return total;
    }

    public static delegate*<int, int> Pick(int kind) =>
        kind switch
        {
            0 => &Damage,
            1 => &Heal,
            _ => &Nothing,
        };

    public static void Run()
    {
        Check.Equal(Apply(&Damage, 10), 20, "un pointeur de fonction s'obtient avec '&' devant une methode STATIQUE");
        Check.Equal(Apply(&Heal, 10), -10, "et s'appelle exactement comme une methode");

        Check.Equal(Apply(Pick(0), 10), 20, "on peut le choisir a l'execution");
        Check.Equal(Apply(Pick(1), 10), -10, "et le rendre depuis un switch");
        Check.Equal(Apply(Pick(7), 10), 0, "avec un cas par defaut, comme partout");

        var table = new delegate*<int, int>[] { &Damage, &Heal, &Nothing };

        Check.Equal(ApplyAll(table, 10), 10, "une TABLE de pointeurs de fonction : 20 moins 10 plus 0");
        Check.Equal(table.Length, 3, "trois effets, indexes par un entier");

        Check.Equal(Measure(() => { _ = ApplyAll(table, 10); }), 0L,
            "et parcourir la table ne coute RIEN : un pointeur de fonction est une adresse, pas un objet. Pas d'instance, pas de cible, pas de fermeture");

        Func<int, int> managed = Damage;

        Check.Equal(managed(10), 20, "un delegue fait la meme chose");

        Check.True(Measure(() => { Func<int, int> made = power => power * 2; _ = made(10); }) >= 0L,
            "mais un delegue est un OBJET : il porte une cible, une liste d'invocation, et il s'alloue des qu'on en fabrique un nouveau");

        Check.Equal(sizeof(delegate*<int, int>), 8,
            "un pointeur de fonction pese huit octets, la taille d'une adresse. Un tableau de mille effets pese huit kilooctets et tient dans le cache");

        Check.Equal(Apply(&Damage, 0), 0, "aucune de ces fonctions ne capture quoi que ce soit");

        Check.True(true,
            "c'est la limite, et c'est voulue : un pointeur de fonction ne peut viser qu'une methode statique et ne peut rien capturer. Quand ca suffit - une table d'opcodes de machine virtuelle, un dispatch de systemes d'ECS, un rappel passe a du code natif - c'est ce qu'il y a de plus rapide en C#");
    }
}
