namespace Csharplings;

public sealed class HealthComponent : Node
{
    public event Action<int> Changed;

    public int Current { get; private set; } = 100;

    public void TakeDamage(int amount)
    {
        Current = (int)Mathf.Max(Current - amount, 0f);
        Changed?.Invoke(Current);
    }
}

public sealed class HealthBar : Node
{
    public List<int> Seen { get; } = new();

    public void Follow(HealthComponent health)
    {
        health.Changed += OnChanged;
    }

    public void Unfollow(HealthComponent health)
    {
        health.Changed -= OnChanged;
    }

    private void OnChanged(int current)
    {
        Seen.Add(current);
    }
}

public static class Godot4
{
    public const bool NotDone = false;

    public static void Run()
    {
        var health = new HealthComponent { Name = "Health" };
        var bar = new HealthBar { Name = "Bar" };

        bar.Follow(health);

        health.TakeDamage(30);
        health.TakeDamage(20);

        Check.Sequence(bar.Seen, new[] { 70, 50 }, "la barre suit chaque changement sans connaitre le joueur");

        bar.Unfollow(health);
        health.TakeDamage(10);

        Check.Sequence(bar.Seen, new[] { 70, 50 }, "apres desabonnement, plus aucune notification");
        Check.Equal(health.Current, 40, "les degats sont bien appliques quand meme");

        var orphan = new HealthComponent { Name = "Orphan" };
        orphan.TakeDamage(5);
        Check.Equal(orphan.Current, 95, "sans aucun abonne, l'event ne doit pas planter");

        orphan.TakeDamage(1000);
        Check.Equal(orphan.Current, 0, "les points de vie ne descendent jamais sous zero");
    }
}
