namespace Csharplings;

public sealed class Cooldown
{
    private float _remaining;

    public Cooldown(float duration)
    {
        Duration = duration;
    }

    public float Duration { get; }
    public bool IsReady => _remaining <= 0f;
    public float Progress => Duration <= 0f ? 1f : 1f - _remaining / Duration;

    public void Tick(double delta)
    {
        _remaining = Mathf.Max(_remaining - (float)delta, 0f);
    }

    public bool TryUse()
    {
        if (!IsReady)
            return false;

        _remaining = Duration;
        return true;
    }
}

public static class Timers1
{
    public const bool NotDone = false;

    public static int ShotsFired(float cooldownDuration, int frames, double delta)
    {
        var gun = new Cooldown(cooldownDuration);
        int shots = 0;

        for (int frame = 0; frame < frames; frame++)
        {
            if (gun.TryUse())
                shots++;

            gun.Tick(delta);
        }

        return shots;
    }

    public static void Run()
    {
        var potion = new Cooldown(1f);

        Check.True(potion.IsReady, "une capacite neuve est disponible");
        Check.True(potion.TryUse(), "on l'utilise une premiere fois");
        Check.False(potion.IsReady, "elle est maintenant en recharge");
        Check.False(potion.TryUse(), "impossible de l'utiliser pendant la recharge");

        potion.Tick(0.5);
        Check.Near(potion.Progress, 0.5, "la barre de recharge est a moitie pleine");
        Check.False(potion.IsReady, "pas encore prete");

        potion.Tick(0.5);
        Check.True(potion.IsReady, "une seconde plus tard, elle est prete");
        Check.True(potion.TryUse(), "et reutilisable");

        Check.Equal(ShotsFired(0.5f, 8, 0.25), 4, "un tir toutes les 2 frames de 0.25 s");
        Check.Equal(ShotsFired(0f, 5, 0.25), 5, "sans recharge, on tire a chaque frame");
    }
}
