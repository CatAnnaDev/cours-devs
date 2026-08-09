using Godot;

namespace Demos.Gameplay;

public partial class DeltaTime : Node2D
{
    [Export] public float PixelsPerSecond { get; set; } = 200f;
    [Export] public float SmoothingPerSecond { get; set; } = 8f;

    private Vector2 _target;
    private double _elapsed;

    public override void _Ready()
    {
        _target = Position + new Vector2(400f, 0f);
    }

    public override void _Process(double delta)
    {
        _elapsed += delta;

        Position += Vector2.Right * PixelsPerSecond * (float)delta;

        float smoothing = 1f - Mathf.Exp(-SmoothingPerSecond * (float)delta);
        Position = Position.Lerp(_target, smoothing);

        Rotation = Mathf.MoveToward(Rotation, Mathf.Pi, 2f * (float)delta);

        if (_elapsed > 1.0)
        {
            _elapsed = 0.0;
            GD.Print($"fps={Engine.GetFramesPerSecond()} pos={Position.X:0} rot={Rotation:0.00}");
        }
    }
}

public partial class WrongDeltaUsage : Node2D
{
    public override void _Process(double delta)
    {
        Position += Vector2.Right * 5f;
    }
}
