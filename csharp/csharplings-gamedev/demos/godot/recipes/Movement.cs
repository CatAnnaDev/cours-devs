using Godot;

namespace Demos.Recipes;

public partial class ChasingEnemy : CharacterBody2D
{
    [Export] public float Speed { get; set; } = 120f;
    [Export] public float StopDistance { get; set; } = 24f;
    [Export] public float TurnStrength { get; set; } = 8f;
    [Export] private Node2D _target;

    public override void _PhysicsProcess(double delta)
    {
        if (!IsInstanceValid(_target))
        {
            Velocity = Vector2.Zero;
            MoveAndSlide();

            return;
        }

        Vector2 self = GlobalPosition;
        Vector2 toTarget = _target.GlobalPosition - self;
        float distance = toTarget.Length();

        Vector2 wanted = distance <= StopDistance
            ? Vector2.Zero
            : toTarget / distance * Speed;

        float weight = 1f - Mathf.Exp(-TurnStrength * (float)delta);

        Velocity = Velocity.Lerp(wanted, weight);

        MoveAndSlide();
    }
}

public partial class Jumper : CharacterBody2D
{
    private static readonly StringName Left = "move_left";
    private static readonly StringName Right = "move_right";
    private static readonly StringName JumpAction = "jump";

    [Export] public float MaxSpeed { get; set; } = 220f;
    [Export] public float Acceleration { get; set; } = 1600f;
    [Export] public float Friction { get; set; } = 1400f;
    [Export] public float JumpHeight { get; set; } = 64f;
    [Export] public float CoyoteTime { get; set; } = 0.12f;
    [Export] public float JumpBuffer { get; set; } = 0.12f;
    [Export] public float ShortHopMultiplier { get; set; } = 2f;

    private float _gravity;
    private float _sinceLeftFloor = float.PositiveInfinity;
    private float _sinceJumpPressed = float.PositiveInfinity;

    public override void _Ready() =>
        _gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

    public override void _PhysicsProcess(double delta)
    {
        float step = (float)delta;
        Vector2 velocity = Velocity;

        _sinceLeftFloor += step;
        _sinceJumpPressed += step;

        if (Input.IsActionJustPressed(JumpAction))
            _sinceJumpPressed = 0f;

        float input = Input.GetAxis(Left, Right);

        velocity.X = Mathf.IsZeroApprox(input)
            ? Mathf.MoveToward(velocity.X, 0f, Friction * step)
            : Mathf.MoveToward(velocity.X, input * MaxSpeed, Acceleration * step);

        if (!IsOnFloor())
        {
            bool rising = velocity.Y < 0f;
            bool released = !Input.IsActionPressed(JumpAction);
            float scale = rising && released ? ShortHopMultiplier : 1f;

            velocity.Y += _gravity * scale * step;
        }

        bool wantsJump = _sinceJumpPressed <= JumpBuffer;
        bool allowed = _sinceLeftFloor <= CoyoteTime;

        if (wantsJump && allowed)
        {
            velocity.Y = -Mathf.Sqrt(2f * _gravity * JumpHeight);
            _sinceJumpPressed = float.PositiveInfinity;
            _sinceLeftFloor = float.PositiveInfinity;
        }

        Velocity = velocity;

        MoveAndSlide();

        if (IsOnFloor())
            _sinceLeftFloor = 0f;
    }
}

public partial class FollowCamera : Camera2D
{
    [Export] private Node2D _target;
    [Export] public float DeadZone { get; set; } = 24f;
    [Export] public float Smoothing { get; set; } = 6f;
    [Export] public float MaxShake { get; set; } = 12f;
    [Export] public float TraumaRecovery { get; set; } = 1.5f;

    private readonly RandomNumberGenerator _rng = new();
    private float _trauma;

    public override void _Ready()
    {
        ProcessPriority = 100;
        _rng.Randomize();
    }

    public void Shake(float amount) => _trauma = Mathf.Min(_trauma + amount, 1f);

    public override void _Process(double delta)
    {
        float step = (float)delta;

        if (IsInstanceValid(_target))
        {
            Vector2 self = GlobalPosition;
            Vector2 toTarget = _target.GlobalPosition - self;
            float distance = toTarget.Length();

            if (distance > DeadZone)
            {
                Vector2 wanted = self + toTarget.Normalized() * (distance - DeadZone);
                float weight = 1f - Mathf.Exp(-Smoothing * step);

                GlobalPosition = self.Lerp(wanted, weight);
            }
        }

        _trauma = Mathf.Max(_trauma - TraumaRecovery * step, 0f);

        Offset = _trauma <= 0f
            ? Vector2.Zero
            : new Vector2(_rng.RandfRange(-1f, 1f), _rng.RandfRange(-1f, 1f)) * MaxShake * _trauma * _trauma;
    }
}
