namespace Csharplings;

public sealed class ComponentColumn<T>
    where T : struct
{
    private int[] _slotByEntity;
    private int[] _entityBySlot;
    private T[] _values;

    public ComponentColumn(int capacity)
    {
        int size = Math.Max(capacity, 4);

        _slotByEntity = new int[size];
        _entityBySlot = new int[size];
        _values = new T[size];
    }

    public int Count { get; private set; }

    public ReadOnlySpan<int> Entities => new ReadOnlySpan<int>(_entityBySlot, 0, Count);

    public Span<T> Values => new Span<T>(_values, 0, Count);

    public bool Has(int entity) =>
        entity >= 0 && entity < _slotByEntity.Length && _slotByEntity[entity] > 0;

    public void Set(int entity, T value)
    {
        if (Has(entity))
        {
            _values[_slotByEntity[entity] - 1] = value;
            return;
        }

        GrowForEntity(entity);
        GrowForSlot(Count + 1);

        _entityBySlot[Count] = entity;
        _values[Count] = value;
        _slotByEntity[entity] = Count + 1;
        Count++;
    }

    public ref T Get(int entity)
    {
        if (!Has(entity))
            throw new KeyNotFoundException($"l'entite {entity} n'a pas ce composant");

        return ref _values[_slotByEntity[entity] - 1];
    }

    public bool Remove(int entity)
    {
        if (!Has(entity))
            return false;

        int slot = _slotByEntity[entity] - 1;
        int last = Count - 1;

        if (slot != last)
        {
            int moved = _entityBySlot[last];

            _values[slot] = _values[last];
            _entityBySlot[slot] = moved;
            _slotByEntity[moved] = slot + 1;
        }

        _values[last] = default;
        _entityBySlot[last] = 0;
        _slotByEntity[entity] = 0;
        Count--;

        return true;
    }

    private void GrowForEntity(int entity)
    {
        if (entity < _slotByEntity.Length)
            return;

        int size = _slotByEntity.Length;

        while (size <= entity)
            size *= 2;

        Array.Resize(ref _slotByEntity, size);
    }

    private void GrowForSlot(int needed)
    {
        if (needed <= _values.Length)
            return;

        int size = _values.Length * 2;

        Array.Resize(ref _values, size);
        Array.Resize(ref _entityBySlot, size);
    }
}

public static class Storage1
{
    public const bool NotDone = false;

    public static void Run()
    {
        var column = new ComponentColumn<Vector2>(4);

        column.Set(3, new Vector2(30f, 0f));
        column.Set(1, new Vector2(10f, 0f));
        column.Set(7, new Vector2(70f, 0f));

        Check.Equal(column.Count, 3, "trois composants ranges");
        Check.True(column.Has(3), "l'entite 3 en a un");
        Check.False(column.Has(4), "l'entite 4 n'en a pas");
        Check.False(column.Has(-1), "un index negatif non plus, et sans planter");
        Check.Near(column.Get(1), new Vector2(10f, 0f), "on retrouve la valeur par index d'entite");

        Check.Sequence(column.Entities.ToArray(), new[] { 3, 1, 7 },
            "les valeurs sont rangees DENSE, dans l'ordre d'insertion, sans trou entre elles");

        column.Get(1) = new Vector2(99f, 99f);

        Check.Near(column.Get(1), new Vector2(99f, 99f), "Get renvoie un 'ref' : on ecrit dans la colonne sans recopier la structure");

        column.Set(1, new Vector2(11f, 0f));

        Check.Equal(column.Count, 3, "reecrire un composant deja la ne rajoute pas une ligne");
        Check.Near(column.Get(1), new Vector2(11f, 0f), "ca remplace la valeur");

        Check.True(column.Remove(3), "on retire le composant de l'entite 3");
        Check.Equal(column.Count, 2, "il en reste deux");
        Check.False(column.Has(3), "l'entite 3 n'en a plus");
        Check.False(column.Remove(3), "et le retirer deux fois ne fait rien");

        Check.Sequence(column.Entities.ToArray(), new[] { 7, 1 },
            "suppression par ECHANGE : la derniere ligne vient boucher le trou, la colonne reste contigue");
        Check.Near(column.Get(7), new Vector2(70f, 0f),
            "et celle qui a demenage se retrouve toujours par son index d'entite : c'est la table slot-par-entite qui a suivi");
        Check.Near(column.Get(1), new Vector2(11f, 0f), "les autres n'ont pas bouge");

        Check.Throws<KeyNotFoundException>(() => column.Get(3), "demander un composant absent leve une erreur claire");

        for (int entity = 0; entity < 64; entity++)
            column.Set(entity, new Vector2(entity, 0f));

        Check.Equal(column.Count, 64, "la colonne s'agrandit toute seule, table creuse comprise");
        Check.Near(column.Get(63), new Vector2(63f, 0f), "sans rien perdre en route");
        Check.Equal(column.Values.Length, 64, "et le Span expose exactement les lignes utilisees, pas la capacite");

        Check.Equal(SumWithSpan(column), 2016f, "la somme de 0 a 63");
        Check.Equal(Report("parcourir la colonne en Span", Allocations(column, SumWithSpan)), 0L,
            "parcourir la colonne ne coute PAS un octet : memoire contigue, aucun objet intermediaire");
        Check.True(Report("le meme calcul en LINQ", Allocations(column, SumWithLinq)) > 0L,
            "le meme calcul en LINQ alloue, et 60 fois par seconde ca finit par se voir");
    }

    private static long Report(string what, long bytes)
    {
        Console.WriteLine($"      mesure  {what} : {bytes} octets");

        return bytes;
    }

    private static float SumWithSpan(ComponentColumn<Vector2> column)
    {
        float total = 0f;
        Span<Vector2> values = column.Values;

        for (int i = 0; i < values.Length; i++)
            total += values[i].X;

        return total;
    }

    private static float SumWithLinq(ComponentColumn<Vector2> column) =>
        column.Values.ToArray().Sum(value => value.X);

    private static long Allocations(ComponentColumn<Vector2> column, Func<ComponentColumn<Vector2>, float> work)
    {
        work(column);
        work(column);
        work(column);

        long before = GC.GetAllocatedBytesForCurrentThread();
        work(column);

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }
}
