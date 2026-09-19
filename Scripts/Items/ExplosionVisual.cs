using Godot;

public partial class ExplosionVisual : Node3D
{
    [Export] public float Duration { get; set; } = 0.35f;
    [Export] public float FinalScale { get; set; } = 3.5f;

    private float _elapsed;

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        float progress = Mathf.Clamp(_elapsed / Duration, 0.0f, 1.0f);
        Scale = Vector3.One * Mathf.Lerp(0.35f, FinalScale, progress);

        if (progress >= 1.0f)
        {
            QueueFree();
        }
    }
}
