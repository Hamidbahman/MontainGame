using Godot;

public partial class PlayerController : CharacterBody3D
{
    [Export] public float MoveSpeed { get; set; } = 5.0f;
    [Export] public float Acceleration { get; set; } = 20.0f;
    [Export] public float Deceleration { get; set; } = 25.0f;
    [Export] public float JumpVelocity { get; set; } = 6.0f;
    [Export] public float Gravity { get; set; } = 18.0f;
    [Export] public float RotationSpeed { get; set; } = 10.0f;
    [Export] public float ClimbSpeed { get; set; } = 3.0f;
    [Export] public float GrabDistance { get; set; } = 1.15f;
    [Export] public float WallOffset { get; set; } = 0.55f;

    private Camera3D _camera = null!;
    private Node3D _visual = null!;
    private RayCast3D _wallDetector = null!;
    private bool _isClimbing;
    private Vector3 _climbNormal;

    public override void _Ready()
    {
        _camera = GetNode<Camera3D>("CameraPivot/SpringArm3D/Camera3D");
        _visual = GetNode<Node3D>("Visual");
        _wallDetector = GetNode<RayCast3D>("Visual/WallDetector");
        _wallDetector.TargetPosition = new Vector3(0.0f, 0.0f, -GrabDistance);
    }

    public override void _PhysicsProcess(double delta)
    {
        float deltaTime = (float)delta;
        Vector3 velocity = Velocity;

        if (Input.IsActionJustPressed("grab"))
        {
            if (_isClimbing)
            {
                StopClimbing();
            }
            else
            {
                TryStartClimbing();
            }
        }

        if (_isClimbing)
        {
            ProcessClimbing();
            return;
        }

        if (!IsOnFloor())
        {
            velocity.Y -= Gravity * deltaTime;
        }

        if (Input.IsActionJustPressed("jump") && IsOnFloor())
        {
            velocity.Y = JumpVelocity;
        }

        Vector2 input = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
        Vector3 movementDirection = GetCameraRelativeDirection(input);
        Vector3 desiredVelocity = movementDirection * MoveSpeed;
        float rate = input == Vector2.Zero ? Deceleration : Acceleration;

        velocity.X = Mathf.MoveToward(velocity.X, desiredVelocity.X, rate * deltaTime);
        velocity.Z = Mathf.MoveToward(velocity.Z, desiredVelocity.Z, rate * deltaTime);

        if (movementDirection != Vector3.Zero)
        {
            float targetYaw = Mathf.Atan2(-movementDirection.X, -movementDirection.Z);
            float turnAmount = Mathf.Min(RotationSpeed * deltaTime, 1.0f);
            _visual.Rotation = new Vector3(
                _visual.Rotation.X,
                Mathf.LerpAngle(_visual.Rotation.Y, targetYaw, turnAmount),
                _visual.Rotation.Z);
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    private Vector3 GetCameraRelativeDirection(Vector2 input)
    {
        if (input == Vector2.Zero)
        {
            return Vector3.Zero;
        }

        Vector3 forward = -_camera.GlobalTransform.Basis.Z;
        Vector3 right = _camera.GlobalTransform.Basis.X;
        forward.Y = 0.0f;
        right.Y = 0.0f;

        return (right.Normalized() * input.X - forward.Normalized() * input.Y).Normalized();
    }

    private void TryStartClimbing()
    {
        _wallDetector.ForceRaycastUpdate();

        if (!_wallDetector.IsColliding() || _wallDetector.GetCollider() is not Node collider || !collider.IsInGroup("climbable"))
        {
            return;
        }

        _climbNormal = _wallDetector.GetCollisionNormal().Normalized();
        Vector3 attachPoint = _wallDetector.GetCollisionPoint() + _climbNormal * WallOffset;
        GlobalPosition = new Vector3(attachPoint.X, GlobalPosition.Y, attachPoint.Z);
        Velocity = Vector3.Zero;
        _isClimbing = true;
        GD.Print("Started climbing");
    }

    private void ProcessClimbing()
    {
        Vector2 input = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
        Vector3 sideways = Vector3.Up.Cross(_climbNormal).Normalized();
        Vector3 climbVelocity = sideways * input.X * ClimbSpeed + Vector3.Up * -input.Y * ClimbSpeed;

        Vector3 faceWallDirection = -_climbNormal;
        float targetYaw = Mathf.Atan2(-faceWallDirection.X, -faceWallDirection.Z);
        _visual.Rotation = new Vector3(_visual.Rotation.X, targetYaw, _visual.Rotation.Z);

        Velocity = climbVelocity;
        MoveAndSlide();
    }

    private void StopClimbing()
    {
        _isClimbing = false;
        GD.Print("Stopped climbing");
    }
}
