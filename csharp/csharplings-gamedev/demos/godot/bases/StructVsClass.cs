using Godot;

namespace Demos.Bases;

public readonly record struct DamageInfo(float Amount, string Source, bool IsCritical);

public struct MutableStats
{
    public int Health;
    public int Mana;
}

public sealed class InventorySlot
{
    public string Item { get; set; }
    public int Quantity { get; set; }
}

public partial class StructVsClass : Node
{
    public override void _Ready()
    {
        CopyVsShare();
        TheClassicStructTrap();
        RecordStructIsUsuallyWhatYouWant();
    }

    private static void CopyVsShare()
    {
        var statsA = new MutableStats { Health = 100, Mana = 50 };
        MutableStats statsB = statsA;
        statsB.Health = 1;

        var slotA = new InventorySlot { Item = "potion", Quantity = 5 };
        InventorySlot slotB = slotA;
        slotB.Quantity = 1;

        GD.Print($"struct : copie    -> A={statsA.Health} B={statsB.Health} (A intact)");
        GD.Print($"class  : partage  -> A={slotA.Quantity} B={slotB.Quantity} (A modifie aussi)");
    }

    private static void TheClassicStructTrap()
    {
        var stats = new MutableStats { Health = 100 };
        HealsNothing(stats);
        GD.Print($"struct passe a une methode -> toujours {stats.Health}, la methode a modifie une copie");

        HealsForReal(ref stats);
        GD.Print($"avec 'ref'                 -> {stats.Health}, la vraie valeur");
    }

    private static void HealsNothing(MutableStats stats) => stats.Health += 50;

    private static void HealsForReal(ref MutableStats stats) => stats.Health += 50;

    private static void RecordStructIsUsuallyWhatYouWant()
    {
        var hit = new DamageInfo(25f, "epee", IsCritical: false);
        DamageInfo crit = hit with { Amount = hit.Amount * 2f, IsCritical = true };

        GD.Print($"record struct : {hit}");
        GD.Print($"'with' cree une copie modifiee : {crit}");
        GD.Print($"egalite par valeur, gratuite : {hit == new DamageInfo(25f, "epee", false)}");
    }
}
