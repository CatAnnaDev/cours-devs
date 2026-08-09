using System;
using Godot;

namespace Demos.Singletons;

public static class DamageMath
{
    public static float ApplyArmor(float rawDamage, float armor) =>
        rawDamage * (100f / (100f + Mathf.Max(armor, 0f)));

    public static float CriticalMultiplier(int criticalLevel) =>
        1.5f + 0.25f * criticalLevel;
}

public static class SceneRoutes
{
    public const string MainMenu = "res://scenes/main_menu.tscn";
    public const string Arena = "res://scenes/arena.tscn";
}

public sealed class DamageRoll
{
    private readonly Random _random;

    public DamageRoll(int seed)
    {
        _random = new Random(seed);
    }

    public float Next(float baseDamage, float spread) =>
        baseDamage + (float)(_random.NextDouble() * 2.0 - 1.0) * spread;
}

public sealed class BadGlobalCounter
{
    private static int _totalEnemiesKilled;

    public static int TotalEnemiesKilled => _totalEnemiesKilled;

    public static void Increment() => _totalEnemiesKilled++;
}

public sealed class RunCounter
{
    public int EnemiesKilled { get; private set; }

    public void Increment() => EnemiesKilled++;

    public void Reset() => EnemiesKilled = 0;
}

public partial class StaticVsInstance : Node
{
    private readonly DamageRoll _deterministicRoll = new(seed: 1234);
    private readonly RunCounter _counter = new();

    public override void _Ready()
    {
        float raw = _deterministicRoll.Next(baseDamage: 20f, spread: 5f);
        float final = DamageMath.ApplyArmor(raw, armor: 40f) * DamageMath.CriticalMultiplier(2);

        _counter.Increment();
        BadGlobalCounter.Increment();

        GD.Print($"final={final:0.00} run={_counter.EnemiesKilled} global={BadGlobalCounter.TotalEnemiesKilled}");
        GD.Print($"next scene would be {SceneRoutes.Arena}");
    }
}
