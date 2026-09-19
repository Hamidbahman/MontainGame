using Godot;

public partial class ThirdPersonCamera : Node3D
{
    [Export] public float MouseSensitivity { get; set; } = 0.003f;
    [Export] public float MinPitch { get; set; } = Mathf.DegToRad(-60.0f);
    [Export] public float MaxPitch { get; set; } = Mathf.DegToRad(35.0f);

    private SpringArm3D _springArm = null!;

    public override void _Ready()
    {
        _springArm = GetNode<SpringArm3D>("SpringArm3D");
        Input.MouseMode = Input.MouseModeEnum.Captured;
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

        float pitch = Mathf.Clamp(
            _springArm.Rotation.X - mouseMotion.Relative.Y * MouseSensitivity,
            MinPitch,
            MaxPitch);
        _springArm.Rotation = new Vector3(pitch, 0.0f, 0.0f);
    }
}
