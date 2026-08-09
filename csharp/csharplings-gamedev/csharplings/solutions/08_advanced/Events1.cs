namespace Csharplings;

public sealed class Wallet
{
    public event Action<int> CoinsChanged;

    public int Coins { get; private set; }

    public void Earn(int amount)
    {
        Coins += amount;
        CoinsChanged?.Invoke(Coins);
    }
}

public static class Events1
{
    public const bool NotDone = false;

    public static void Run()
    {
        var wallet = new Wallet();
        var received = new List<int>();

        void OnCoinsChanged(int total) => received.Add(total);

        wallet.CoinsChanged += OnCoinsChanged;

        wallet.Earn(10);
        wallet.Earn(5);

        Check.Sequence(received, new[] { 10, 15 }, "l'auditeur recoit le nouveau total a chaque fois");

        wallet.CoinsChanged -= OnCoinsChanged;
        wallet.Earn(100);

        Check.Sequence(received, new[] { 10, 15 }, "apres -= on ne recoit plus rien");
        Check.Equal(wallet.Coins, 115, "mais le portefeuille a bien encaisse");

        var silent = new Wallet();
        silent.Earn(1);
        Check.Equal(silent.Coins, 1, "sans aucun auditeur, l'event ne doit pas planter");
    }
}
