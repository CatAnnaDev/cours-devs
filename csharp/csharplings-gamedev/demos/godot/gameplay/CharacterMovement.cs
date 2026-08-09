using Godot;

namespace Demos.Gameplay;

public partial class CharacterMovement : CharacterBody2D
{
    private static readonly StringName MoveLeft = "move_left";
    private static readonly StringName MoveRight = "move_right";
    private static readonly StringName Jump = "jump";

    [Export] public float MaxSpeed { get; set; } = 220f;
    [Export] public float Acceleration { get; set; } = 1600f;
    [Export] public float Friction { get; set; } = 1400f;
    [Export] public float JumpHeight { get; set; } = 64f;
    [Export] public float CoyoteTime { get; set; } = 0.12f;
    [Export] public float JumpBuffer { get; set; } = 0.12f;
    [Export] public float FallMultiplier { get; set; } = 1.8f;

    private float _gravity;
    private float _sinceLeftFloor = float.PositiveInfinity;
    private float _sinceJumpPressed = float.PositiveInfinity;

    public override void _Ready()
    {
        _gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
        FloorMaxAngle = Mathf.DegToRad(46f);
    }

    public override void _PhysicsProcess(double delta)
    {
        float step = (float)delta;
        Vector2 velocity = Velocity;

        TrackTimers(step);
        ApplyHorizontal(ref velocity, step);
        ApplyGravity(ref velocity, step);
        TryJump(ref velocity);

        Velocity = velocity;

        MoveAndSlide();

        if (IsOnFloor())
            _sinceLeftFloor = 0f;
    }

    private void TrackTimers(float step)
    {
        _sinceLeftFloor += step;
        _sinceJumpPressed += step;

        if (Input.IsActionJustPressed(Jump))
            _sinceJumpPressed = 0f;
    }

    private void ApplyHorizontal(ref Vector2 velocity, float step)
    {
        float input = Input.GetAxis(MoveLeft, MoveRight);

        velocity.X = Mathf.IsZeroApprox(input)
            ? Mathf.MoveToward(velocity.X, 0f, Friction * step)
            : Mathf.MoveToward(velocity.X, input * MaxSpeed, Acceleration * step);
    }

    private void ApplyGravity(ref Vector2 velocity, float step)
    {
        if (IsOnFloor())
            return;

        float scale = velocity.Y > 0f ? FallMultiplier : 1f;

        velocity.Y += _gravity * scale * step;

        if (velocity.Y < 0f && !Input.IsActionPressed(Jump))
            velocity.Y += _gravity * step;
    }

    private void TryJump(ref Vector2 velocity)
    {
        bool wantsJump = _sinceJumpPressed <= JumpBuffer;
        bool allowedToJump = _sinceLeftFloor <= CoyoteTime;

        if (!wantsJump || !allowedToJump)
            return;

        velocity.Y = -Mathf.Sqrt(2f * _gravity * JumpHeight);

        _sinceJumpPressed = float.PositiveInfinity;
        _sinceLeftFloor = float.PositiveInfinity;
    }

    public string DescribeContacts()
    {
        if (!IsOnFloor())
            return "en l'air";

        float slope = Mathf.RadToDeg(GetFloorNormal().AngleTo(Vector2.Up));

        return $"au sol, pente {slope:0.#} degres, {GetSlideCollisionCount()} contacts";
    }
}
