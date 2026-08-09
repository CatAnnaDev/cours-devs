namespace Csharplings;

public interface IDamageable
{
    bool IsAlive { get; }
    void TakeDamage(int amount);
}

public sealed class Crate : IDamageable
{
    private int _integrity = 30;

    public bool IsAlive => _integrity > 0;

    public void TakeDamage(int amount)
    {
        _integrity -= amount;
    }
}

public sealed class Ghost : IDamageable
{
    public bool IsAlive => true;

    public void TakeDamage(int amount)
    {
    }
}

public static class Interfaces1
{
    public const bool NotDone = false;

    public static int DestroyAll(List<IDamageable> targets, int hitPower)
    {
        int destroyed = 0;

        foreach (IDamageable target in targets)
        {
            target.TakeDamage(hitPower);

            if (!target.IsAlive)
                destroyed++;
        }

        return destroyed;
    }

    public static void Run()
    {
        var crate = new Crate();

        Check.True(crate.IsAlive, "une caisse neuve est intacte");
        crate.TakeDamage(10);
        Check.True(crate.IsAlive, "10 degats ne suffisent pas");
        crate.TakeDamage(25);
        Check.False(crate.IsAlive, "35 degats au total la detruisent");

        var targets = new List<IDamageable> { new Crate(), new Ghost(), new Crate() };
        Check.Equal(DestroyAll(targets, 100), 2, "le fantome survit, les deux caisses non");
    }
}
