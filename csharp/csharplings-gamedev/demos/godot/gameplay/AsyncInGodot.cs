using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace Demos.Gameplay;

public partial class AsyncInGodot : Node2D
{
    private readonly CancellationTokenSource _lifetime = new();

    public override void _Ready()
    {
        _ = RunIntroSequence(_lifetime.Token);
    }

    public override void _ExitTree()
    {
        _lifetime.Cancel();
        _lifetime.Dispose();
    }

    private async Task RunIntroSequence(CancellationToken token)
    {
        GD.Print("sequence : debut");

        await Wait(1.0);
        if (token.IsCancellationRequested || !IsInstanceValid(this))
            return;

        GD.Print("sequence : 1 seconde plus tard, toujours vivant");

        await FadeTo(0f, 0.5);
        if (token.IsCancellationRequested || !IsInstanceValid(this))
            return;

        GD.Print("sequence : fondu termine");

        await ToSignal(this, Node.SignalName.TreeExiting);
        GD.Print("on n'arrive jamais ici en pratique");
    }

    private SignalAwaiter Wait(double seconds) =>
        ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);

    private async Task FadeTo(float alpha, double duration)
    {
        Tween tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", alpha, duration);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private async Task<int> LoadHeavyThingWithoutFreezingTheGame()
    {
        int result = await Task.Run(() =>
        {
            int sum = 0;
            for (int i = 0; i < 50_000_000; i++)
                sum += i % 7;
            return sum;
        });

        GD.Print($"calcul lourd fini hors du thread principal : {result}");
        return result;
    }
}

public partial class TweenInsteadOfManualLerp : Node2D
{
    public override void _Ready()
    {
        Tween tween = CreateTween().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);

        tween.TweenProperty(this, "position", new Vector2(400f, 0f), 0.6);
        tween.Parallel().TweenProperty(this, "scale", Vector2.One * 1.5f, 0.6);
        tween.TweenProperty(this, "modulate:a", 0f, 0.3);
        tween.TweenCallback(Callable.From(QueueFree));
    }
}
