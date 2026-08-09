using System;
using Godot;

namespace Demos.Gameplay;

public partial class Emitter : Node
{
    [Signal] public delegate void CoinCollectedEventHandler(int value);

    public event Action<int> CoinCollectedCSharp;

    public void Collect(int value)
    {
        EmitSignal(SignalName.CoinCollected, value);
        CoinCollectedCSharp?.Invoke(value);
    }
}

public partial class SignalsVsEvents : Node
{
    private Emitter _emitter;

    public override void _Ready()
    {
        _emitter = new Emitter { Name = "Emitter" };
        AddChild(_emitter);

        _emitter.CoinCollected += OnCoinSignal;
        _emitter.CoinCollectedCSharp += OnCoinEvent;
        _emitter.CoinCollected += value => GD.Print($"lambda inline : {value}");

        _emitter.Connect(Emitter.SignalName.CoinCollected, Callable.From((int v) => GD.Print($"Connect() : {v}")));

        _emitter.Collect(25);

        GD.Print($"nb de connexions au signal : {_emitter.GetSignalConnectionList(Emitter.SignalName.CoinCollected).Count}");
    }

    public override void _ExitTree()
    {
        if (_emitter == null || !IsInstanceValid(_emitter))
            return;

        _emitter.CoinCollected -= OnCoinSignal;
        _emitter.CoinCollectedCSharp -= OnCoinEvent;
    }

    private void OnCoinSignal(int value) => GD.Print($"signal Godot : +{value}");

    private void OnCoinEvent(int value) => GD.Print($"event C# : +{value}");
}
