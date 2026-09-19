using Godot;

public partial class PlayerController : CharacterBody3D
{
    [Export] public int PlayerId { get; set; } = 1;
    [Export] public float MoveSpeed { get; set; } = 5.0f;
    [Export] public float Acceleration { get; set; } = 20.0f;
    [Export] public float Deceleration { get; set; } = 25.0f;
    [Export] public float JumpVelocity { get; set; } = 6.0f;
    [Export] public float Gravity { get; set; } = 18.0f;
    [Export] public float RotationSpeed { get; set; } = 10.0f;
    [Export] public float ClimbSpeed { get; set; } = 3.0f;
    [Export] public float GrabDistance { get; set; } = 1.15f;
    [Export] public float WallOffset { get; set; } = 0.55f;
    [Export] public float LedgeDetectionHeight { get; set; } = 1.6f;
    [Export] public float LedgeGrabOffset { get; set; } = 0.9f;
    [Export] public float LedgeClimbOffset { get; set; } = 1.0f;

    private Camera3D _camera = null;
    private Node3D _visual = null!;
    private RayCast3D _wallDetector = null!;
    private RayCast3D _ledgeDetector = null!;
    private RayCast3D _ledgeTopDetector = null!;
    private bool _isClimbing;
    private bool _isHanging;
    private Vector3 _climbNormal;
    private Vector3 _ledgeTopPoint;

    public override void _Ready()
    {
        _visual = GetNode<Node3D>("Visual");
        _wallDetector = GetNode<RayCast3D>("Visual/WallDetector");
        _ledgeDetector = GetNode<RayCast3D>("Visual/LedgeDetector");
        _ledgeTopDetector = GetNode<RayCast3D>("Visual/LedgeTopDetector");
        _wallDetector.AddException(this);
        _ledgeDetector.AddException(this);
        _ledgeTopDetector.AddException(this);
        _wallDetector.TargetPosition = new Vector3(0.0f, 0.0f, -GrabDistance);
        _ledgeDetector.Position = new Vector3(0.0f, LedgeDetectionHeight, 0.0f);
        _ledgeDetector.TargetPosition = new Vector3(0.0f, 0.0f, -GrabDistance);
        _ledgeTopDetector.Position = new Vector3(0.0f, LedgeDetectionHeight + 0.2f, -WallOffset - 0.2f);
    }

    public override void _PhysicsProcess(double delta)
    {
        _camera ??= GetTree().GetFirstNodeInGroup("shared_camera") as Camera3D;
        if (_camera is null)
        {
            return;
        }

        float deltaTime = (float)delta;
        Vector3 velocity = Velocity;

        if (Input.IsActionJustPressed(ActionName("grab")))
        {
            if (_isHanging)
            {
                ReleaseLedge();
            }
            else if (_isClimbing)
            {
                StopClimbing();
            }
            else
            {
                TryStartClimbing();
            }
        }

        if (_isHanging)
        {
            ProcessHanging();
            return;
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

        if (Input.IsActionJustPressed(ActionName("jump")) && IsOnFloor())
        {
            velocity.Y = JumpVelocity;
        }

        Vector2 input = GetMovementInput();
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
        AimWallDetectorAtCamera();
        _wallDetector.ForceRaycastUpdate();

        if (!_wallDetector.IsColliding() || _wallDetector.GetCollider() is not Node collider || !collider.IsInGroup("climbable"))
        {
            GD.Print("No climbable wall in grab range");
            return;
        }

        _climbNormal = _wallDetector.GetCollisionNormal().Normalized();
        Vector3 attachPoint = _wallDetector.GetCollisionPoint() + _climbNormal * WallOffset;
        GlobalPosition = new Vector3(attachPoint.X, GlobalPosition.Y, attachPoint.Z);
        FaceWall();
        Velocity = Vector3.Zero;
        _isClimbing = true;
        GD.Print("Started climbing");
    }

    private void ProcessClimbing()
    {
        if (!TryGetClimbableWall(out Vector3 wallNormal, out Vector3 wallPoint))
        {
            StopClimbing();
            return;
        }

        _climbNormal = wallNormal;

        if (TryGetLedge(out Vector3 ledgeTopPoint))
        {
            BeginHanging(ledgeTopPoint);
            return;
        }

        MaintainWallContact(wallPoint);

        Vector2 input = GetMovementInput();
        Vector3 sideways = Vector3.Up.Cross(_climbNormal).Normalized();
        Vector3 climbVelocity = sideways * input.X * ClimbSpeed + Vector3.Up * -input.Y * ClimbSpeed;

        FaceWall();

        Velocity = climbVelocity;
        MoveAndSlide();
    }

    private bool TryGetClimbableWall(out Vector3 normal, out Vector3 point)
    {
        _wallDetector.ForceRaycastUpdate();

        if (_wallDetector.IsColliding() && _wallDetector.GetCollider() is Node collider && collider.IsInGroup("climbable"))
        {
            normal = _wallDetector.GetCollisionNormal().Normalized();
            point = _wallDetector.GetCollisionPoint();
            return true;
        }

        normal = Vector3.Zero;
        point = Vector3.Zero;
        return false;
    }

    private bool TryGetLedge(out Vector3 topPoint)
    {
        _ledgeDetector.ForceRaycastUpdate();
        _ledgeTopDetector.ForceRaycastUpdate();

        bool wallEndsAbovePlayer = !_ledgeDetector.IsColliding();
        bool hasClimbableTop = _ledgeTopDetector.IsColliding()
            && _ledgeTopDetector.GetCollider() is Node collider
            && collider.IsInGroup("climbable")
            && _ledgeTopDetector.GetCollisionNormal().Y > 0.7f;

        if (wallEndsAbovePlayer && hasClimbableTop)
        {
            topPoint = _ledgeTopDetector.GetCollisionPoint();
            return true;
        }

        topPoint = Vector3.Zero;
        return false;
    }

    private void MaintainWallContact(Vector3 wallPoint)
    {
        Vector3 attachedPosition = wallPoint + _climbNormal * WallOffset;
        GlobalPosition = new Vector3(attachedPosition.X, GlobalPosition.Y, attachedPosition.Z);
    }

    private void BeginHanging(Vector3 ledgeTopPoint)
    {
        _ledgeTopPoint = ledgeTopPoint;
        _isHanging = true;
        Velocity = Vector3.Zero;
        HoldAtLedge();
        GD.Print("Grabbed ledge");
    }

    private void ProcessHanging()
    {
        HoldAtLedge();

        if (Input.IsActionJustPressed(ActionName("move_forward")))
        {
            ClimbOntoLedge();
        }
    }

    private void HoldAtLedge()
    {
        Vector3 hangPosition = _ledgeTopPoint + _climbNormal * WallOffset - Vector3.Up * LedgeGrabOffset;
        GlobalPosition = hangPosition;
        Velocity = Vector3.Zero;
    }

    private void ClimbOntoLedge()
    {
        GlobalPosition = _ledgeTopPoint - _climbNormal * LedgeClimbOffset + Vector3.Up * LedgeGrabOffset;
        Velocity = Vector3.Zero;
        _isHanging = false;
        _isClimbing = false;
        GD.Print("Climbed onto ledge");
    }

    private void StopClimbing()
    {
        _isClimbing = false;
        GD.Print("Stopped climbing");
    }

    private void ReleaseLedge()
    {
        _isHanging = false;
        _isClimbing = false;
        Velocity = Vector3.Zero;
        GD.Print("Released ledge");
    }

    private void AimWallDetectorAtCamera()
    {
        Vector3 cameraForward = -_camera!.GlobalTransform.Basis.Z;
        cameraForward.Y = 0.0f;

        if (cameraForward.LengthSquared() > 0.0f)
        {
            float cameraYaw = Mathf.Atan2(-cameraForward.X, -cameraForward.Z);
            _wallDetector.GlobalRotation = new Vector3(0.0f, cameraYaw, 0.0f);
        }
    }

    private void FaceWall()
    {
        Vector3 faceWallDirection = -_climbNormal;
        float targetYaw = Mathf.Atan2(-faceWallDirection.X, -faceWallDirection.Z);
        _visual.Rotation = new Vector3(_visual.Rotation.X, targetYaw, _visual.Rotation.Z);
        _wallDetector.Rotation = Vector3.Zero;
    }

    private Vector2 GetMovementInput()
    {
        return Input.GetVector(
            ActionName("move_left"),
            ActionName("move_right"),
            ActionName("move_forward"),
            ActionName("move_backward"));
    }

    private string ActionName(string action)
    {
        return PlayerId == 2 ? $"p2_{action}" : $"p1_{action}";
    }
}
