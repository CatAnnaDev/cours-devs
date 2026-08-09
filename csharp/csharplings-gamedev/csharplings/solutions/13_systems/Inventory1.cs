namespace Csharplings;

public sealed record ItemKind(string Id, int MaxStack);

public sealed class Slot
{
    public ItemKind Kind { get; set; }
    public int Count { get; set; }

    public bool IsEmpty => Kind is null || Count <= 0;
}

public sealed class Inventory
{
    private readonly Slot[] _slots;

    public Inventory(int size)
    {
        _slots = new Slot[size];

        for (int i = 0; i < size; i++)
            _slots[i] = new Slot();
    }

    public int Size => _slots.Length;

    public Slot this[int index] => _slots[index];

    public int CountOf(ItemKind kind)
    {
        int total = 0;

        foreach (Slot slot in _slots)
        {
            if (!slot.IsEmpty && slot.Kind == kind)
                total += slot.Count;
        }

        return total;
    }

    public int Add(ItemKind kind, int count)
    {
        foreach (Slot slot in _slots)
        {
            if (count == 0)
                break;

            if (slot.IsEmpty || slot.Kind != kind)
                continue;

            int moved = Mathf.Min(kind.MaxStack - slot.Count, count);
            slot.Count += moved;
            count -= moved;
        }

        foreach (Slot slot in _slots)
        {
            if (count == 0)
                break;

            if (!slot.IsEmpty)
                continue;

            int moved = Mathf.Min(kind.MaxStack, count);
            slot.Kind = kind;
            slot.Count = moved;
            count -= moved;
        }

        return count;
    }

    public bool Remove(ItemKind kind, int count)
    {
        if (CountOf(kind) < count)
            return false;

        for (int i = _slots.Length - 1; i >= 0 && count > 0; i--)
        {
            Slot slot = _slots[i];

            if (slot.IsEmpty || slot.Kind != kind)
                continue;

            int moved = Mathf.Min(slot.Count, count);
            slot.Count -= moved;
            count -= moved;

            if (slot.Count == 0)
                slot.Kind = null;
        }

        return true;
    }
}

public static class Inventory1
{
    public const bool NotDone = false;

    public static void Run()
    {
        var potion = new ItemKind("potion", 10);
        var sword = new ItemKind("epee", 1);

        var bag = new Inventory(3);

        Check.Equal(bag.Add(potion, 7), 0, "sept potions tiennent dans un sac vide");
        Check.Equal(bag.CountOf(potion), 7, "et on les retrouve");
        Check.Equal(bag[0].Count, 7, "toutes dans la premiere case");
        Check.True(bag[1].IsEmpty, "les autres cases restent libres");

        Check.Equal(bag.Add(potion, 8), 0, "huit de plus");
        Check.Equal(bag[0].Count, 10, "on remplit d'abord la pile existante jusqu'au maximum");
        Check.Equal(bag[1].Count, 5, "puis on entame une nouvelle case");
        Check.Equal(bag.CountOf(potion), 15, "quinze potions en tout");

        Check.Equal(bag.Add(sword, 1), 0, "une epee prend la derniere case");
        Check.Equal(bag.Add(sword, 4), 4, "et il ne reste plus de place : quatre epees rendues");
        Check.Equal(bag.CountOf(sword), 1, "le sac n'a pas avale ce qu'il ne pouvait pas ranger");

        Check.True(bag.Remove(potion, 12), "on peut retirer plus que le contenu d'une seule pile");
        Check.Equal(bag.CountOf(potion), 3, "il en reste trois");
        Check.True(bag[1].IsEmpty, "une pile videe libere sa case");

        Check.False(bag.Remove(potion, 99), "retirer plus qu'on n'a doit echouer");
        Check.Equal(bag.CountOf(potion), 3, "et surtout ne rien retirer du tout");

        Check.True(bag.Remove(potion, 3), "on vide le reste");
        Check.Equal(bag.CountOf(potion), 0, "plus rien");
        Check.True(bag[0].IsEmpty, "et la case est de nouveau libre");
    }
}
