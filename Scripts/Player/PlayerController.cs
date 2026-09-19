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
    [Export] public float GripStrength { get; set; } = 100.0f;
    [Export] public float ForcedReleaseCooldown { get; set; } = 0.3f;
    [Export] public float KickRange { get; set; } = 1.0f;
    [Export] public float KickForce { get; set; } = 10.0f;
    [Export] public float KickUpwardForce { get; set; } = 3.0f;
    [Export] public float KickCooldown { get; set; } = 0.6f;
    [Export] public float KnockdownImpulseThreshold { get; set; } = 80.0f;
    [Export] public float FallKnockdownSpeed { get; set; } = 12.0f;
    [Export] public float KnockdownDuration { get; set; } = 2.0f;
    [Export] public float RagdollLinearDamp { get; set; } = 0.4f;
    [Export] public float RagdollAngularDamp { get; set; } = 0.8f;
    [Export] public float PlayerGrabRange { get; set; } = 1.25f;
    [Export] public float PlayerGrabDistance { get; set; } = 0.9f;
    [Export] public float PlayerGrabStiffness { get; set; } = 30.0f;
    [Export] public float PlayerGrabDamping { get; set; } = 8.0f;
    [Export] public float MaxPlayerGrabForce { get; set; } = 120.0f;
    [Export] public float PlayerGrabBreakForce { get; set; } = 100.0f;
    [Export] public float PlayerSupportCarryFactor { get; set; } = 1.0f;
    [Export] public float LandingReactionMultiplier { get; set; } = 0.15f;
    [Export] public float JumpOffReaction { get; set; } = 0.35f;
    [Export] public bool IsClimbing { get; private set; }
    [Export] public bool IsHanging { get; private set; }
    [Export] public float CurrentRopeLoad { get; private set; }
    [Export] public bool IsKnockedDown { get; private set; }
    [Export] public bool IsGrabbingPlayer { get; private set; }

    private Camera3D _camera = null;
    private Node3D _visual = null!;
    private RayCast3D _wallDetector = null!;
    private RayCast3D _ledgeDetector = null!;
    private RayCast3D _ledgeTopDetector = null!;
    private Area3D _kickDetector = null!;
    private CollisionShape3D _kickShape = null!;
    private Area3D _playerGrabDetector = null!;
    private CollisionShape3D _playerGrabShape = null!;
    private CollisionShape3D _controllerCollision = null!;
    private RigidBody3D _ragdollBody = null!;
    private MeshInstance3D _ragdollVisual = null!;
    private Vector3 _climbNormal;
    private Vector3 _ledgeTopPoint;
    private Vector3 _externalVelocity;
    private float _forcedReleaseRemaining;
    private float _kickCooldownRemaining;
    private float _knockdownRemaining;
    private Node _tractionSource;
    private float _tractionAccelerationMultiplier = 1.0f;
    private float _tractionDecelerationMultiplier = 1.0f;
    private PlayerController _grabbedPlayer = null;
    private PlayerController _grabbedBy = null;
    private PlayerController _standingOnPlayer = null;

    public PlayerController GrabbedPlayer => _grabbedPlayer;

    public override void _Ready()
    {
        _visual = GetNode<Node3D>("Visual");
        _controllerCollision = GetNode<CollisionShape3D>("CollisionShape3D");
        _ragdollBody = GetNode<RigidBody3D>("PrototypeRagdollBody");
        _ragdollVisual = GetNode<MeshInstance3D>("PrototypeRagdollBody/MeshInstance3D");
        _wallDetector = GetNode<RayCast3D>("Visual/WallDetector");
        _ledgeDetector = GetNode<RayCast3D>("Visual/LedgeDetector");
        _ledgeTopDetector = GetNode<RayCast3D>("Visual/LedgeTopDetector");
        _kickDetector = GetNode<Area3D>("Visual/KickDetector");
        _kickShape = GetNode<CollisionShape3D>("Visual/KickDetector/CollisionShape3D");
        _playerGrabDetector = GetNode<Area3D>("Visual/PlayerGrabDetector");
        _playerGrabShape = GetNode<CollisionShape3D>("Visual/PlayerGrabDetector/CollisionShape3D");
        _wallDetector.AddException(this);
        _ledgeDetector.AddException(this);
        _ledgeTopDetector.AddException(this);
        _wallDetector.TargetPosition = new Vector3(0.0f, 0.0f, -GrabDistance);
        _ledgeDetector.Position = new Vector3(0.0f, LedgeDetectionHeight, 0.0f);
        _ledgeDetector.TargetPosition = new Vector3(0.0f, 0.0f, -GrabDistance);
        _ledgeTopDetector.Position = new Vector3(0.0f, LedgeDetectionHeight + 0.2f, -WallOffset - 0.2f);
        ConfigureKickDetector();
        ConfigurePlayerGrabDetector();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsKnockedDown)
        {
            ReleasePlayerGrab();
            ProcessKnockdown((float)delta);
            return;
        }

        _camera ??= GetTree().GetFirstNodeInGroup("shared_camera") as Camera3D;
        if (_camera is null)
        {
            return;
        }

        float deltaTime = (float)delta;
        _forcedReleaseRemaining = Mathf.Max(0.0f, _forcedReleaseRemaining - deltaTime);
        _kickCooldownRemaining = Mathf.Max(0.0f, _kickCooldownRemaining - deltaTime);
        Vector3 externalVelocity = _externalVelocity;
        _externalVelocity = Vector3.Zero;
        Vector3 velocity = Velocity + externalVelocity;

        if (_standingOnPlayer is { IsKnockedDown: false })
        {
            Vector3 supportVelocity = _standingOnPlayer.Velocity;
            velocity.X += supportVelocity.X * PlayerSupportCarryFactor;
            velocity.Z += supportVelocity.Z * PlayerSupportCarryFactor;
        }
        else
        {
            _standingOnPlayer = null;
        }

        if (IsGrabbingPlayer && Input.IsActionJustReleased(ActionName("grab")))
        {
            ReleasePlayerGrab();
        }
        else if (!IsGrabbingPlayer && Input.IsActionJustPressed(ActionName("grab")) && _forcedReleaseRemaining <= 0.0f)
        {
            if (!TryStartPlayerGrab() && IsHanging)
            {
                ReleaseLedge();
            }
            else if (!IsGrabbingPlayer && IsClimbing)
            {
                StopClimbing();
            }
            else if (!IsGrabbingPlayer)
            {
                TryStartClimbing();
            }
        }

        if (IsGrabbingPlayer)
        {
            ProcessPlayerGrab(deltaTime);
        }

        if (Input.IsActionJustPressed(ActionName("kick")) && _kickCooldownRemaining <= 0.0f)
        {
            TryKick();
        }

        if (IsHanging)
        {
            ProcessHanging(externalVelocity);
            return;
        }

        if (IsClimbing)
        {
            ProcessClimbing(externalVelocity);
            return;
        }

        if (!IsOnFloor())
        {
            velocity.Y -= Gravity * deltaTime;
        }

        if (Input.IsActionJustPressed(ActionName("jump")) && IsOnFloor())
        {
            if (_standingOnPlayer is not null)
            {
                _standingOnPlayer.ApplyExternalVelocity(Vector3.Down * JumpOffReaction);
                GD.Print($"Player{PlayerId} jumped from Player{_standingOnPlayer.PlayerId}");
                _standingOnPlayer = null;
            }

            velocity.Y = JumpVelocity;
        }

        Vector2 input = GetMovementInput();
        Vector3 movementDirection = GetCameraRelativeDirection(input);
        Vector3 desiredVelocity = movementDirection * MoveSpeed;
        float rate = input == Vector2.Zero
            ? Deceleration * _tractionDecelerationMultiplier
            : Acceleration * _tractionAccelerationMultiplier;

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

        float fallSpeed = velocity.Y;
        Velocity = velocity;
        MoveAndSlide();
        UpdatePlayerSupport(fallSpeed);

        if (IsOnFloor() && fallSpeed <= -FallKnockdownSpeed)
        {
            EnterKnockdown(Vector3.Zero);
        }
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

    private void UpdatePlayerSupport(float fallSpeed)
    {
        PlayerController previousSupport = _standingOnPlayer;
        _standingOnPlayer = null;

        for (int index = 0; index < GetSlideCollisionCount(); index++)
        {
            KinematicCollision3D collision = GetSlideCollision(index);
            PlayerController support = collision.GetCollider() as PlayerController;
            if (support is null || support == this || support.IsKnockedDown || collision.GetNormal().Y < 0.65f)
            {
                continue;
            }

            _standingOnPlayer = support;
            if (previousSupport != support && fallSpeed < -0.5f)
            {
                float impactSpeed = -fallSpeed;
                support.ApplyHazardImpulse(
                    Vector3.Down * impactSpeed * LandingReactionMultiplier,
                    impactSpeed * 8.0f);
                GD.Print($"Player{PlayerId} landed on Player{support.PlayerId}");
            }

            return;
        }
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
        IsClimbing = true;
        GD.Print("Started climbing");
    }

    private void ProcessClimbing(Vector3 externalVelocity)
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
        climbVelocity += externalVelocity - _climbNormal * externalVelocity.Dot(_climbNormal);

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
        IsHanging = true;
        Velocity = Vector3.Zero;
        HoldAtLedge();
        GD.Print("Grabbed ledge");
    }

    private void ProcessHanging(Vector3 externalVelocity)
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
        IsHanging = false;
        IsClimbing = false;
        GD.Print("Climbed onto ledge");
    }

    private void StopClimbing()
    {
        IsClimbing = false;
        GD.Print("Stopped climbing");
    }

    private void ReleaseLedge()
    {
        IsHanging = false;
        IsClimbing = false;
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

    public void ApplyExternalVelocity(Vector3 velocityChange)
    {
        if (IsKnockedDown)
        {
            _ragdollBody.LinearVelocity += velocityChange;
            return;
        }

        _externalVelocity += velocityChange;
    }

    public void ApplyRopeTension(Vector3 velocityChange, float tension)
    {
        CurrentRopeLoad = tension;
        TryBreakGrip(velocityChange, tension);
        ReceiveImpulse(velocityChange, tension);
    }

    public void ApplyKickImpulse(Vector3 velocityChange, float gripLoad)
    {
        TryBreakGrip(velocityChange, gripLoad);
        ReceiveImpulse(velocityChange, gripLoad);
    }

    public void ApplyHazardImpulse(Vector3 velocityChange, float impactStrength)
    {
        TryBreakGrip(velocityChange, impactStrength);
        ReceiveImpulse(velocityChange, impactStrength);
    }

    public void SetSurfaceTraction(Node source, float accelerationMultiplier, float decelerationMultiplier)
    {
        _tractionSource = source;
        _tractionAccelerationMultiplier = accelerationMultiplier;
        _tractionDecelerationMultiplier = decelerationMultiplier;
    }

    public void ClearSurfaceTraction(Node source)
    {
        if (_tractionSource != source)
        {
            return;
        }

        _tractionSource = null;
        _tractionAccelerationMultiplier = 1.0f;
        _tractionDecelerationMultiplier = 1.0f;
        _standingOnPlayer = null;
    }

    private void TryKick()
    {
        foreach (Node3D body in _kickDetector.GetOverlappingBodies())
        {
            if (body is not PlayerController target || target == this)
            {
                continue;
            }

            Vector3 forward = -_visual.GlobalTransform.Basis.Z;
            forward.Y = 0.0f;
            Vector3 kickVelocity = forward.Normalized() * KickForce + Vector3.Up * KickUpwardForce;
            target.ApplyKickImpulse(kickVelocity, KickForce * 12.0f);
            ApplyExternalVelocity(-forward.Normalized() * 0.5f);
            _kickCooldownRemaining = KickCooldown;
            GD.Print($"Player{PlayerId} kicked Player{target.PlayerId}");
            return;
        }
    }

    private void ConfigureKickDetector()
    {
        if (_kickShape.Shape is not BoxShape3D shape)
        {
            return;
        }

        float range = Mathf.Max(0.1f, KickRange);
        shape.Size = new Vector3(0.9f, 1.2f, range);
        _kickDetector.Position = new Vector3(0.0f, 0.9f, -0.45f - range * 0.5f);
    }

    private void ConfigurePlayerGrabDetector()
    {
        if (_playerGrabShape.Shape is not BoxShape3D shape)
        {
            return;
        }

        float range = Mathf.Max(0.1f, PlayerGrabRange);
        shape.Size = new Vector3(1.0f, 1.2f, range);
        _playerGrabDetector.Position = new Vector3(0.0f, 0.9f, -0.45f - range * 0.5f);
    }

    private void TryBreakGrip(Vector3 velocityChange, float load)
    {
        if ((!IsClimbing && !IsHanging) || _forcedReleaseRemaining > 0.0f || load <= GripStrength)
        {
            return;
        }

        Vector3 pullDirection = velocityChange.Normalized();
        bool ropePushesIntoWall = pullDirection.Dot(_climbNormal) < -0.25f;
        if (ropePushesIntoWall)
        {
            return;
        }

        IsClimbing = false;
        IsHanging = false;
        _standingOnPlayer = null;
        _forcedReleaseRemaining = ForcedReleaseCooldown;
        GD.Print($"GRIP BROKEN: load={load:0.0} strength={GripStrength:0.0}");
    }

    private bool TryStartPlayerGrab()
    {
        foreach (Node3D body in _playerGrabDetector.GetOverlappingBodies())
        {
            PlayerController target = body as PlayerController ?? body.GetParent() as PlayerController;
            if (target is null || target == this || target._grabbedBy is not null)
            {
                continue;
            }

            _grabbedPlayer = target;
            _grabbedPlayer._grabbedBy = this;
            IsGrabbingPlayer = true;
            return true;
        }

        return false;
    }

    private void ProcessPlayerGrab(float deltaTime)
    {
        if (_grabbedPlayer is null)
        {
            ReleasePlayerGrab();
            return;
        }

        Vector3 offset = _grabbedPlayer.GetRopeAnchorPosition(0.7f) - GetRopeAnchorPosition(0.7f);
        float distance = offset.Length();
        if (distance < 0.001f || distance <= PlayerGrabDistance)
        {
            return;
        }

        Vector3 direction = offset / distance;
        float stretch = distance - PlayerGrabDistance;
        float separatingSpeed = (_grabbedPlayer.GetRopeVelocity() - GetRopeVelocity()).Dot(direction);
        float grabForce = Mathf.Min(
            stretch * PlayerGrabStiffness + Mathf.Max(0.0f, separatingSpeed) * PlayerGrabDamping,
            MaxPlayerGrabForce);

        if (grabForce > PlayerGrabBreakForce)
        {
            ReleasePlayerGrab(true);
            return;
        }

        Vector3 velocityChange = direction * grabForce * deltaTime;
        ApplyExternalVelocity(velocityChange);
        _grabbedPlayer.ApplyExternalVelocity(-velocityChange);
    }

    private void ReleasePlayerGrab(bool broken = false)
    {
        if (!IsGrabbingPlayer)
        {
            return;
        }

        if (_grabbedPlayer is not null && _grabbedPlayer._grabbedBy == this)
        {
            _grabbedPlayer._grabbedBy = null;
        }

        _grabbedPlayer = null;
        IsGrabbingPlayer = false;

        if (broken)
        {
            GD.Print("PLAYER GRAB BROKEN");
        }
    }

    private void BreakPlayerGrabForImpulse(float strength)
    {
        if (strength <= PlayerGrabBreakForce)
        {
            return;
        }

        ReleasePlayerGrab(true);
        _grabbedBy?.ReleasePlayerGrab(true);
    }

    public Vector3 GetRopeAnchorPosition(float anchorHeight)
    {
        return (IsKnockedDown ? _ragdollBody.GlobalPosition : GlobalPosition) + Vector3.Up * anchorHeight;
    }

    public Vector3 GetRopeVelocity()
    {
        return IsKnockedDown ? _ragdollBody.LinearVelocity : Velocity;
    }

    public void ResetTo(Vector3 position)
    {
        ReleasePlayerGrab();
        _grabbedBy?.ReleasePlayerGrab();
        IsClimbing = false;
        IsHanging = false;
        IsKnockedDown = false;
        CurrentRopeLoad = 0.0f;
        _externalVelocity = Vector3.Zero;
        _forcedReleaseRemaining = 0.0f;
        _kickCooldownRemaining = 0.0f;
        _knockdownRemaining = 0.0f;
        _tractionSource = null;
        _tractionAccelerationMultiplier = 1.0f;
        _tractionDecelerationMultiplier = 1.0f;
        _ragdollBody.Freeze = true;
        _ragdollBody.CollisionLayer = 0;
        _ragdollBody.LinearVelocity = Vector3.Zero;
        _ragdollBody.AngularVelocity = Vector3.Zero;
        _ragdollBody.Position = Vector3.Zero;
        _ragdollBody.Rotation = Vector3.Zero;
        _ragdollVisual.Visible = false;
        _visual.Visible = true;
        CollisionLayer = 1;
        _controllerCollision.SetDeferred("disabled", false);
        GlobalPosition = position;
        GlobalRotation = Vector3.Zero;
        Velocity = Vector3.Zero;
    }

    private void ReceiveImpulse(Vector3 velocityChange, float strength)
    {
        BreakPlayerGrabForImpulse(strength);

        if (IsKnockedDown)
        {
            _ragdollBody.LinearVelocity += velocityChange;
            return;
        }

        if (strength >= KnockdownImpulseThreshold)
        {
            EnterKnockdown(velocityChange);
            return;
        }

        _externalVelocity += velocityChange;
    }

    private void EnterKnockdown(Vector3 entryImpulse)
    {
        if (IsKnockedDown)
        {
            return;
        }

        IsClimbing = false;
        IsHanging = false;
        ReleasePlayerGrab();
        IsKnockedDown = true;
        _knockdownRemaining = KnockdownDuration;

        _ragdollBody.GlobalTransform = GlobalTransform;
        _ragdollBody.LinearVelocity = Velocity + _externalVelocity + entryImpulse;
        _ragdollBody.AngularVelocity = Vector3.Up.Cross(entryImpulse) * 0.8f;
        _ragdollBody.LinearDamp = RagdollLinearDamp;
        _ragdollBody.AngularDamp = RagdollAngularDamp;
        _ragdollBody.Freeze = false;
        _ragdollBody.CollisionLayer = 1;
        _ragdollVisual.Visible = true;
        _visual.Visible = false;
        CollisionLayer = 0;
        _controllerCollision.SetDeferred("disabled", true);
        _externalVelocity = Vector3.Zero;
        GD.Print($"Player{PlayerId} KNOCKED DOWN");
    }

    private void ProcessKnockdown(float deltaTime)
    {
        _knockdownRemaining -= deltaTime;

        bool hasGroundContact = _ragdollBody.GetContactCount() > 0;
        bool isSettled = Mathf.Abs(_ragdollBody.LinearVelocity.Y) < 3.0f;
        if (_knockdownRemaining > 0.0f || !hasGroundContact || !isSettled)
        {
            return;
        }

        GlobalPosition = _ragdollBody.GlobalPosition;
        GlobalRotation = new Vector3(0.0f, _ragdollBody.GlobalRotation.Y, 0.0f);
        Velocity = _ragdollBody.LinearVelocity;
        _ragdollBody.Freeze = true;
        _ragdollBody.CollisionLayer = 0;
        _ragdollBody.LinearVelocity = Vector3.Zero;
        _ragdollBody.AngularVelocity = Vector3.Zero;
        _ragdollBody.Position = Vector3.Zero;
        _ragdollBody.Rotation = Vector3.Zero;
        _ragdollVisual.Visible = false;
        _visual.Visible = true;
        CollisionLayer = 1;
        _controllerCollision.SetDeferred("disabled", false);
        IsKnockedDown = false;
        GD.Print($"Player{PlayerId} RECOVERED");
    }
}
