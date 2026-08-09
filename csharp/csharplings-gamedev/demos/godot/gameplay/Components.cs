using Godot;

namespace Demos.Gameplay;

public interface IDamageable
{
    void TakeDamage(float amount, Node source);
    bool IsAlive { get; }
}

public partial class HealthComponent : Node, IDamageable
{
    [Export] public float MaxHealth { get; set; } = 100f;
    [Export] public bool Invulnerable { get; set; }

    [Signal] public delegate void HealthChangedEventHandler(float current, float max);
    [Signal] public delegate void DiedEventHandler();

    private float _current;

    public float Current => _current;
    public bool IsAlive => _current > 0f;
    public float Ratio => MaxHealth <= 0f ? 0f : _current / MaxHealth;

    public override void _Ready()
    {
        _current = MaxHealth;
    }

    public void TakeDamage(float amount, Node source)
    {
        if (Invulnerable || !IsAlive || amount <= 0f)
            return;

        _current = Mathf.Max(_current - amount, 0f);
        EmitSignal(SignalName.HealthChanged, _current, MaxHealth);
        GD.Print($"{GetParent().Name} prend {amount} de {source?.Name ?? "?"} -> {_current}/{MaxHealth}");

        if (!IsAlive)
            EmitSignal(SignalName.Died);
    }

    public void Heal(float amount)
    {
        if (!IsAlive || amount <= 0f)
            return;

        _current = Mathf.Min(_current + amount, MaxHealth);
        EmitSignal(SignalName.HealthChanged, _current, MaxHealth);
    }
}

public partial class HurtboxComponent : Area2D
{
    [Export] private HealthComponent _health;
    [Export] private float _damagePerHit = 10f;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    public override void _ExitTree()
    {
        BodyEntered -= OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        _health?.TakeDamage(_damagePerHit, body);
    }
}

public partial class HealthBar : ProgressBar
{
    [Export] private HealthComponent _health;

    public override void _Ready()
    {
        if (_health == null)
            return;

        _health.HealthChanged += OnHealthChanged;
        OnHealthChanged(_health.Current, _health.MaxHealth);
    }

    public override void _ExitTree()
    {
        if (_health != null && IsInstanceValid(_health))
            _health.HealthChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(float current, float max)
    {
        MaxValue = max;
        Value = current;
    }
}

public partial class ComponentUser : CharacterBody2D
{
    private HealthComponent _health;

    public override void _Ready()
    {
        _health = GetNodeOrNull<HealthComponent>("HealthComponent");

        if (_health != null)
            _health.Died += OnDied;
    }

    public override void _ExitTree()
    {
        if (_health != null && IsInstanceValid(_health))
            _health.Died -= OnDied;
    }

    private void OnDied()
    {
        GD.Print($"{Name} est mort, je me retire proprement");
        QueueFree();
    }
}
