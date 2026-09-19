using Godot;

public partial class SharedCamera : Node3D
{
    [Export] public NodePath Player1Path { get; set; } = new NodePath("");
    [Export] public NodePath Player2Path { get; set; } = new NodePath("");
    [Export] public float FollowHeight { get; set; } = 1.5f;
    [Export] public float FollowSpeed { get; set; } = 8.0f;
    [Export] public float BaseDistance { get; set; } = 7.0f;
    [Export] public float DistancePerPlayerSeparation { get; set; } = 0.5f;
    [Export] public float MinDistance { get; set; } = 6.0f;
    [Export] public float MaxDistance { get; set; } = 12.0f;
    [Export] public float VerticalSeparationOffset { get; set; } = 0.25f;
    [Export] public float MouseSensitivity { get; set; } = 0.003f;
    [Export] public float MinPitch { get; set; } = Mathf.DegToRad(-55.0f);
    [Export] public float MaxPitch { get; set; } = Mathf.DegToRad(30.0f);
    [Export] public float CameraShakeStrength { get; set; } = 0.18f;
    [Export] public float CameraShakeDuration { get; set; } = 0.2f;

    private Node3D _player1 = null!;
    private Node3D _player2 = null!;
    private SpringArm3D _springArm = null!;
    private float _shakeRemaining;
    private float _shakeAmount;
    private float _shakeElapsed;

    public override void _Ready()
    {
        _player1 = GetNode<Node3D>(Player1Path);
        _player2 = GetNode<Node3D>(Player2Path);
        _springArm = GetNode<SpringArm3D>("SpringArm3D");
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Process(double delta)
    {
        Vector3 midpoint = (_player1.GlobalPosition + _player2.GlobalPosition) * 0.5f;
        float verticalSeparation = Mathf.Abs(_player1.GlobalPosition.Y - _player2.GlobalPosition.Y);
        Vector3 targetPosition = midpoint + Vector3.Up * (FollowHeight + verticalSeparation * VerticalSeparationOffset);
        Vector3 shakeOffset = Vector3.Zero;
        if (_shakeRemaining > 0.0f)
        {
            _shakeRemaining -= (float)delta;
            _shakeElapsed += (float)delta;
            float fade = Mathf.Clamp(_shakeRemaining / CameraShakeDuration, 0.0f, 1.0f);
            shakeOffset = new Vector3(
                Mathf.Sin(_shakeElapsed * 83.0f),
                Mathf.Cos(_shakeElapsed * 67.0f),
                0.0f) * (_shakeAmount * fade);
        }

        GlobalPosition = GlobalPosition.Lerp(targetPosition + shakeOffset, Mathf.Min(FollowSpeed * (float)delta, 1.0f));

        float separation = _player1.GlobalPosition.DistanceTo(_player2.GlobalPosition);
        _springArm.SpringLength = Mathf.Clamp(
            BaseDistance + separation * DistancePerPlayerSeparation,
            MinDistance,
            MaxDistance);
    }

    public void AddShake(float proximity)
    {
        _shakeAmount = Mathf.Max(_shakeAmount, CameraShakeStrength * Mathf.Clamp(proximity, 0.0f, 1.0f));
        _shakeRemaining = CameraShakeDuration;
        _shakeElapsed = 0.0f;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Keycode == Key.Escape && keyEvent.Pressed)
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
            return;
        }

        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed && Input.MouseMode != Input.MouseModeEnum.Captured)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
            return;
        }

        if (@event is not InputEventMouseMotion mouseMotion || Input.MouseMode != Input.MouseModeEnum.Captured)
        {
            return;
        }

        RotateY(-mouseMotion.Relative.X * MouseSensitivity);
        float pitch = Mathf.Clamp(_springArm.Rotation.X - mouseMotion.Relative.Y * MouseSensitivity, MinPitch, MaxPitch);
        _springArm.Rotation = new Vector3(pitch, 0.0f, 0.0f);
    }
}
