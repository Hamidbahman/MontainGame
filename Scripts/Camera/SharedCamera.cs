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
    [Export] public float MouseSensitivity { get; set; } = 0.003f;
    [Export] public float MinPitch { get; set; } = Mathf.DegToRad(-55.0f);
    [Export] public float MaxPitch { get; set; } = Mathf.DegToRad(30.0f);

    private Node3D _player1 = null!;
    private Node3D _player2 = null!;
    private SpringArm3D _springArm = null!;

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
        Vector3 targetPosition = midpoint + Vector3.Up * FollowHeight;
        GlobalPosition = GlobalPosition.Lerp(targetPosition, Mathf.Min(FollowSpeed * (float)delta, 1.0f));

        float separation = _player1.GlobalPosition.DistanceTo(_player2.GlobalPosition);
        _springArm.SpringLength = Mathf.Clamp(
            BaseDistance + separation * DistancePerPlayerSeparation,
            MinDistance,
            MaxDistance);
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
