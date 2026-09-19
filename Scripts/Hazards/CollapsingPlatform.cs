using Godot;

public partial class CollapsingPlatform : Node3D, IResettableHazard
{
    [Export] public float CollapseDelay { get; set; } = 1.0f;
    [Export] public float RespawnDelay { get; set; } = 4.0f;

    private StaticBody3D _platformBody = null!;
    private CollisionShape3D _platformCollision = null!;
    private MeshInstance3D _platformMesh = null!;
    private RigidBody3D _fallingBody = null!;
    private CollisionShape3D _fallingCollision = null!;
    private MeshInstance3D _fallingMesh = null!;
    private bool _triggered;
    private bool _collapsed;
    private float _remaining;

    public override void _Ready()
    {
        _platformBody = GetNode<StaticBody3D>("PlatformBody");
        _platformCollision = GetNode<CollisionShape3D>("PlatformBody/CollisionShape3D");
        _platformMesh = GetNode<MeshInstance3D>("PlatformBody/MeshInstance3D");
        _fallingBody = GetNode<RigidBody3D>("FallingBody");
        _fallingCollision = GetNode<CollisionShape3D>("FallingBody/CollisionShape3D");
        _fallingMesh = GetNode<MeshInstance3D>("FallingBody/MeshInstance3D");
        GetNode<Area3D>("TriggerArea").BodyEntered += OnTriggerBodyEntered;
        ResetHazard();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_triggered && !_collapsed)
        {
            return;
        }

        _remaining -= (float)delta;
        if (!_collapsed && _remaining <= 0.0f)
        {
            Collapse();
        }
        else if (_collapsed && _remaining <= 0.0f)
        {
            ResetHazard();
        }
    }

    public void ResetHazard()
    {
        _triggered = false;
        _collapsed = false;
        _remaining = 0.0f;
        _platformMesh.Visible = true;
        _platformBody.CollisionLayer = 1;
        _platformCollision.SetDeferred("disabled", false);
        _fallingBody.Freeze = true;
        _fallingBody.CollisionLayer = 0;
        _fallingBody.LinearVelocity = Vector3.Zero;
        _fallingBody.AngularVelocity = Vector3.Zero;
        _fallingBody.Position = Vector3.Zero;
        _fallingBody.Rotation = Vector3.Zero;
        _fallingMesh.Visible = false;
        _fallingCollision.SetDeferred("disabled", true);
    }

    private void OnTriggerBodyEntered(Node3D body)
    {
        if (_triggered || _collapsed || ResolvePlayer(body) is null)
        {
            return;
        }

        _triggered = true;
        _remaining = CollapseDelay;
        GD.Print("Collapsing platform triggered");
    }

    private void Collapse()
    {
        _collapsed = true;
        _remaining = RespawnDelay;
        _platformMesh.Visible = false;
        _platformBody.CollisionLayer = 0;
        _platformCollision.SetDeferred("disabled", true);
        _fallingBody.Freeze = false;
        _fallingBody.CollisionLayer = 1;
        _fallingMesh.Visible = true;
        _fallingCollision.SetDeferred("disabled", false);
    }

    private static PlayerController ResolvePlayer(Node node)
    {
        return node as PlayerController ?? node.GetParent() as PlayerController;
    }
}
