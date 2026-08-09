namespace Csharplings;

public sealed class Label : Node
{
    public string Text { get; set; } = string.Empty;
}

public sealed class HealthBar : Node
{
    public int Current { get; set; } = 100;

    public override void _Process(double delta)
    {
        GetNode<Label>("Label").Text = $"PV {Current}";
    }
}

public sealed class StatSheet
{
    private readonly List<int> _bonuses = new();

    public int Recomputes { get; private set; }

    public int BaseAttack { get; set; } = 10;

    public int Attack
    {
        get
        {
            Recomputes++;

            int total = BaseAttack;

            foreach (int bonus in _bonuses)
                total += bonus;

            return total;
        }
    }

    public void AddBonus(int bonus)
    {
        _bonuses.Add(bonus);
    }
}

public static class Cache1
{
    public const bool NotDone = true;

    public static void Run()
    {
        Node.LookupCount = 0;

        var tree = new SceneTree();
        var bar = new HealthBar { Name = "Bar" };
        bar.AddChild(new Label { Name = "Label" });
        tree.Root.AddChild(bar);
        tree.Start();

        int afterReady = Node.LookupCount;

        Check.True(afterReady <= 2, "on cherche le noeud une fois, au demarrage");

        tree.Run(60);

        Check.Equal(Node.LookupCount, afterReady, "et plus jamais ensuite : 60 frames, zero recherche dans l'arbre");
        Check.Equal(bar.GetNode<Label>("Label").Text, "PV 100", "la barre affiche quand meme la bonne valeur");

        var stats = new StatSheet();

        Check.Equal(stats.Attack, 10, "l'attaque de base");
        Check.Equal(stats.Recomputes, 1, "premier acces : on calcule");

        for (int i = 0; i < 100; i++)
        {
            int read = stats.Attack;
        }

        Check.Equal(stats.Recomputes, 1, "cent lectures de plus, toujours un seul calcul");

        stats.AddBonus(5);

        Check.Equal(stats.Attack, 15, "un buff change le total");
        Check.Equal(stats.Recomputes, 2, "et provoque exactement un recalcul");

        for (int i = 0; i < 100; i++)
        {
            int read = stats.Attack;
        }

        Check.Equal(stats.Recomputes, 2, "puis on repart sur le cache");

        stats.BaseAttack = 20;

        Check.Equal(stats.Attack, 25, "changer la base doit invalider le cache aussi");
        Check.Equal(stats.Recomputes, 3, "un recalcul de plus, pas deux");
    }
}
