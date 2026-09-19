using Godot;

public partial class WindZone3D : Area3D
{
    [Export] public Vector3 WindDirection { get; set; } = Vector3.Left;
    [Export] public float Strength { get; set; } = 3.5f;
    [Export(PropertyHint.Range, "0.0,1.0,0.05")] public float Variation { get; set; } = 0.3f;
    [Export] public float GustFrequency { get; set; } = 0.8f;

    private float _elapsed;

    public override void _PhysicsProcess(double delta)
    {
        _elapsed += (float)delta;
        if (WindDirection.LengthSquared() < 0.001f)
        {
            return;
        }

        float gust = 1.0f + Mathf.Sin(_elapsed * GustFrequency * Mathf.Tau) * Variation;
        Vector3 velocityChange = WindDirection.Normalized() * Strength * gust * (float)delta;

        foreach (Node3D body in GetOverlappingBodies())
        {
            PlayerController player = body as PlayerController ?? body.GetParent() as PlayerController;
            player?.ApplyExternalVelocity(velocityChange);
        }
    }
}
