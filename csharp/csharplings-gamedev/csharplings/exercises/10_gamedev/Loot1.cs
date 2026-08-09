namespace Csharplings;

public sealed class LootTable
{
    private readonly List<string> _names = new();
    private readonly List<int> _cumulative = new();

    public int Total => _cumulative.Count == 0 ? 0 : _cumulative[^1];

    public int Count => _names.Count;

    public void Add(string name, int weight)
    {
        _names.Add(name);
        _cumulative.Add(Total + weight);
    }

    public string Roll(int roll)
    {
        if (Total == 0)
            return null;

        int target = Math.Abs(roll) % Total;
        int low = 0;
        int high = _cumulative.Count - 1;

        while (low < high)
        {
            int middle = (low + high) / 2;

            if (_cumulative[middle] < target)
                low = middle + 1;
            else
                high = middle;
        }

        return _names[low];
    }
}

public sealed class PityRoll
{
    private int _failures;

    public PityRoll(int guaranteeAfter)
    {
        GuaranteeAfter = guaranteeAfter;
    }

    public int GuaranteeAfter { get; }

    public int Failures => _failures;

    public bool Roll(int percentChance, int roll)
    {
        if (_failures >= GuaranteeAfter || Math.Abs(roll) % 100 < percentChance)
            return true;

        _failures++;

        return false;
    }
}

public static class Loot1
{
    public const bool NotDone = true;

    public static void Run()
    {
        var table = new LootTable();

        table.Add("rien", 70);
        table.Add("potion", 25);
        table.Add("gemme", 5);

        Check.Equal(table.Total, 100, "les poids CUMULES : 70, puis 95, puis 100");
        Check.Equal(table.Count, 3, "trois entrees");

        Check.Equal(table.Roll(0), "rien", "un tirage a zero tombe dans la premiere tranche");
        Check.Equal(table.Roll(69), "rien", "le dernier de la tranche aussi");
        Check.Equal(table.Roll(70), "potion", "et le suivant bascule : les bornes sont l'endroit ou tout le monde se trompe d'un cran");
        Check.Equal(table.Roll(94), "potion", "fin de la deuxieme tranche");
        Check.Equal(table.Roll(95), "gemme", "debut de la troisieme");
        Check.Equal(table.Roll(99), "gemme", "et sa fin");

        var counts = new Dictionary<string, int>();

        for (int roll = 0; roll < 1000; roll++)
        {
            string drop = table.Roll(roll);

            counts[drop] = counts.GetValueOrDefault(drop) + 1;
        }

        Check.Equal(counts["rien"], 700, "sur mille tirages balayes, la repartition tombe exactement sur les poids");
        Check.Equal(counts["potion"], 250, "un quart de potions");
        Check.Equal(counts["gemme"], 50, "et cinq pour cent de gemmes");

        Check.Throws<ArgumentOutOfRangeException>(() => table.Add("bug", 0),
            "un poids nul est refuse : il ne sortirait jamais, mais surtout il fabrique une tranche VIDE que la recherche peut atteindre");

        Check.Equal(new LootTable().Roll(5), null, "une table vide rend null au lieu de sortir des bornes");

        var pity = new PityRoll(guaranteeAfter: 10);

        Check.False(pity.Roll(percentChance: 5, roll: 50), "un tirage rate");
        Check.Equal(pity.Failures, 1, "et le compteur monte");

        Check.True(pity.Roll(percentChance: 5, roll: 2), "un tirage reussi passe");
        Check.Equal(pity.Failures, 0, "et REMET le compteur a zero : l'oublier rendrait la pitie permanente au bout d'une heure de jeu");

        for (int i = 0; i < 10; i++)
            pity.Roll(percentChance: 5, roll: 50);

        Check.Equal(pity.Failures, 10, "dix echecs d'affilee");

        Check.True(pity.Roll(percentChance: 5, roll: 50),
            "et le onzieme tirage est GARANTI, avec le meme jet perdant qu'avant. C'est ce qui empeche un joueur malchanceux de faire cinquante tentatives et d'aller ecrire que le jeu est casse");

        Check.Equal(pity.Failures, 0, "la garantie remet le compteur a zero comme une vraie reussite");

        int wins = 0;

        var honest = new PityRoll(guaranteeAfter: 10);

        for (int roll = 0; roll < 100; roll++)
        {
            if (honest.Roll(percentChance: 5, roll: roll))
                wins++;
        }

        Check.True(wins > 5,
            $"sur cent tirages a 5 pour cent, la pitie fait monter le taux reel a {wins} pour cent. Une table de butin annoncee a 5 pour cent avec pitie n'est PAS a 5 pour cent, et c'est bon a savoir avant de l'ecrire dans un menu");
    }
}
