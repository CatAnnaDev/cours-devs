using System;
using System.Collections.Generic;

namespace Demos.WeakRefs;

public sealed class WeakCache<TKey, TValue>
    where TValue : class
{
    private readonly Dictionary<TKey, WeakReference<TValue>> _entries = new();
    private readonly Func<TKey, TValue> _factory;
    private int _missesSinceSweep;

    public WeakCache(Func<TKey, TValue> factory)
    {
        _factory = factory;
    }

    public TValue Get(TKey key)
    {
        if (_entries.TryGetValue(key, out WeakReference<TValue> slot)
            && slot.TryGetTarget(out TValue cached))
            return cached;

        TValue created = _factory(key);
        _entries[key] = new WeakReference<TValue>(created);

        if (++_missesSinceSweep >= 32)
            Sweep();

        return created;
    }

    public int Sweep()
    {
        _missesSinceSweep = 0;

        List<TKey> dead = null;
        foreach ((TKey key, WeakReference<TValue> slot) in _entries)
        {
            if (slot.TryGetTarget(out _))
                continue;

            dead ??= new List<TKey>();
            dead.Add(key);
        }

        if (dead == null)
            return 0;

        foreach (TKey key in dead)
            _entries.Remove(key);

        return dead.Count;
    }
}
