using Godot;

public partial class WorldItem : Area3D
{
    [Export] public ItemDefinition Item { get; set; }
    [Export] public float BobHeight { get; set; } = 0.12f;
    [Export] public float BobSpeed { get; set; } = 2.0f;
    [Export] public float RotationSpeed { get; set; } = 1.2f;

    private float _baseHeight;
    private float _elapsed;

    public override void _Ready()
    {
        _baseHeight = Position.Y;
        BodyEntered += OnBodyEntered;
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        Position = new Vector3(Position.X, _baseHeight + Mathf.Sin(_elapsed * BobSpeed) * BobHeight, Position.Z);
        RotateY(RotationSpeed * (float)delta);
    }

    private void OnBodyEntered(Node3D body)
    {
        PlayerController player = body as PlayerController ?? body.GetParent() as PlayerController;
        if (player is null || !player.TryPickUpItem(Item))
        {
            return;
        }

        Monitoring = false;
        QueueFree();
    }
}
