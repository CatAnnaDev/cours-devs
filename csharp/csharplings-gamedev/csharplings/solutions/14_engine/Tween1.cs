namespace Csharplings;

public sealed class Tween
{
    private readonly float _from;
    private readonly float _to;
    private readonly float _duration;
    private readonly Func<float, float> _ease;

    private float _elapsed;

    public Tween(float from, float to, float duration, Func<float, float> ease)
    {
        _from = from;
        _to = to;
        _duration = duration;
        _ease = ease;

        Value = from;
    }

    public float Value { get; private set; }
    public bool Finished { get; private set; }
    public Action Completed { get; set; }

    public void Tick(float delta)
    {
        if (Finished)
            return;

        _elapsed += delta;

        float t = Mathf.Clamp(_elapsed / _duration, 0f, 1f);
        Value = Mathf.Lerp(_from, _to, _ease(t));

        if (t < 1f)
            return;

        Value = _to;
        Finished = true;
        Completed?.Invoke();
    }
}

public static class Tween1
{
    public const bool NotDone = false;

    public static void Run()
    {
        var linear = new Tween(0f, 100f, 1f, t => t);

        Check.Near(linear.Value, 0.0, "un tween commence a sa valeur de depart");
        Check.False(linear.Finished, "et n'est pas fini");

        linear.Tick(0.5f);

        Check.Near(linear.Value, 50.0, "a mi-parcours, la moitie du chemin");
        Check.False(linear.Finished, "toujours en cours");

        linear.Tick(10f);

        Check.Near(linear.Value, 100.0, "un pas trop grand n'emmene PAS au dela de la cible");
        Check.True(linear.Finished, "et le tween est fini");

        int completions = 0;
        var notifying = new Tween(0f, 1f, 0.1f, t => t) { Completed = () => completions++ };

        notifying.Tick(0.05f);

        Check.Equal(completions, 0, "pas encore arrive");

        notifying.Tick(0.05f);

        Check.Equal(completions, 1, "le callback part au moment ou on touche la fin");

        notifying.Tick(1f);
        notifying.Tick(1f);

        Check.Equal(completions, 1, "et une seule fois, meme si on continue a l'animer");
        Check.Near(notifying.Value, 1.0, "la valeur reste posee sur la cible");

        var eased = new Tween(0f, 100f, 1f, t => 1f - (1f - t) * (1f - t));

        eased.Tick(0.5f);

        Check.True(eased.Value > 50f, "un ease-out a deja fait plus de la moitie a mi-temps");
        Check.Near(eased.Value, 75.0, "75 pour cent, pour cette courbe la");

        var backwards = new Tween(200f, 50f, 1f, t => t);

        backwards.Tick(0.5f);

        Check.Near(backwards.Value, 125.0, "un tween marche aussi vers le bas");

        backwards.Tick(0.5f);

        Check.Near(backwards.Value, 50.0, "et atterrit exactement, pas a 50,03");
    }
}
