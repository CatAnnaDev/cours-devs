using System;
using System.Collections.Generic;

namespace Demos.WeakRefs;

public sealed class WeakEventBus<TPayload>
{
    private readonly List<Subscription> _subscriptions = new();

    private sealed class Subscription
    {
        public WeakReference<object> Owner { get; init; }
        public Action<object, TPayload> Invoke { get; init; }
    }

    public void Subscribe<TOwner>(TOwner owner, Action<TOwner, TPayload> handler)
        where TOwner : class
    {
        _subscriptions.Add(new Subscription
        {
            Owner = new WeakReference<object>(owner),
            Invoke = (o, payload) => handler((TOwner)o, payload),
        });
    }

    public void Unsubscribe(object owner)
    {
        _subscriptions.RemoveAll(s => s.Owner.TryGetTarget(out object o) && ReferenceEquals(o, owner));
    }

    public void Publish(TPayload payload)
    {
        for (int i = _subscriptions.Count - 1; i >= 0; i--)
        {
            if (!_subscriptions[i].Owner.TryGetTarget(out object owner))
            {
                _subscriptions.RemoveAt(i);
                continue;
            }

            _subscriptions[i].Invoke(owner, payload);
        }
    }
}
