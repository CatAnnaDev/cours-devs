using System;
using System.Collections.Generic;
using Godot;

namespace Demos.Gameplay;

public interface IState
{
    void Enter();
    void Update(double delta);
    void Exit();
}

public sealed class StateMachine<TKey>
    where TKey : notnull
{
    private readonly Dictionary<TKey, IState> _states = new();

    public TKey CurrentKey { get; private set; }
    public IState Current { get; private set; }

    public event Action<TKey, TKey> Changed;

    public void Register(TKey key, IState state) => _states[key] = state;

    public void ChangeTo(TKey key)
    {
        if (!_states.TryGetValue(key, out IState next))
            throw new ArgumentException($"etat inconnu : {key}");

        if (ReferenceEquals(next, Current))
            return;

        TKey previousKey = CurrentKey;
        Current?.Exit();
        Current = next;
        CurrentKey = key;
        Current.Enter();

        Changed?.Invoke(previousKey, key);
    }

    public void Update(double delta) => Current?.Update(delta);
}

public enum EnemyMood
{
    Idle,
    Chase,
    Attack,
    Flee,
}

public sealed class IdleState : IState
{
    public void Enter() => GD.Print("idle : je regarde le paysage");
    public void Update(double delta) { }
    public void Exit() => GD.Print("idle : fini");
}

public sealed class ChaseState : IState
{
    private readonly Node2D _self;
    private readonly Node2D _target;

    public ChaseState(Node2D self, Node2D target)
    {
        _self = self;
        _target = target;
    }

    public void Enter() => GD.Print("chase : je te vois");

    public void Update(double delta)
    {
        if (!GodotObject.IsInstanceValid(_target))
            return;

        Vector2 direction = (_target.GlobalPosition - _self.GlobalPosition).Normalized();
        _self.GlobalPosition += direction * 150f * (float)delta;
    }

    public void Exit() => GD.Print("chase : j'abandonne");
}

public partial class EnemyBrain : Node2D
{
    [Export] private Node2D _player;

    private readonly StateMachine<EnemyMood> _machine = new();

    public override void _Ready()
    {
        _machine.Register(EnemyMood.Idle, new IdleState());
        _machine.Register(EnemyMood.Chase, new ChaseState(this, _player));
        _machine.Changed += (from, to) => GD.Print($"transition {from} -> {to}");
        _machine.ChangeTo(EnemyMood.Idle);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (GodotObject.IsInstanceValid(_player))
        {
            float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);
            _machine.ChangeTo(distance < 300f ? EnemyMood.Chase : EnemyMood.Idle);
        }

        _machine.Update(delta);
    }
}

public partial class SwitchStateMachine : Node2D
{
    private EnemyMood _mood = EnemyMood.Idle;
    private double _timeInState;

    public override void _PhysicsProcess(double delta)
    {
        _timeInState += delta;

        switch (_mood)
        {
            case EnemyMood.Idle when _timeInState > 2.0:
                SetMood(EnemyMood.Chase);
                break;
            case EnemyMood.Chase when _timeInState > 5.0:
                SetMood(EnemyMood.Flee);
                break;
            case EnemyMood.Flee when _timeInState > 1.5:
                SetMood(EnemyMood.Idle);
                break;
        }
    }

    private void SetMood(EnemyMood mood)
    {
        _mood = mood;
        _timeInState = 0.0;
        GD.Print($"switch simple -> {mood}");
    }
}
