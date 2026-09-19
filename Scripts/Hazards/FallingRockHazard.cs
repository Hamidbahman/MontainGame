using Godot;

public partial class FallingRockHazard : Node3D, IResettableHazard
{
    [Export] public float LaunchDelay { get; set; } = 0.15f;
    [Export] public float ImpactVelocity { get; set; } = 11.0f;
    [Export] public float ImpactStrength { get; set; } = 110.0f;
    [Export] public float ResetDelay { get; set; } = 5.0f;

    private RigidBody3D _rockBody = null!;
    private CollisionShape3D _rockCollision = null!;
    private bool _launched;
    private float _launchRemaining;
    private float _resetRemaining;

    public override void _Ready()
    {
        _rockBody = GetNode<RigidBody3D>("RockBody");
        _rockCollision = GetNode<CollisionShape3D>("RockBody/CollisionShape3D");
        GetNode<Area3D>("TriggerArea").BodyEntered += OnTriggerBodyEntered;
        _rockBody.BodyEntered += OnRockBodyEntered;
        ResetHazard();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_launched)
        {
            if (_launchRemaining > 0.0f)
            {
                _launchRemaining -= (float)delta;
                if (_launchRemaining <= 0.0f)
                {
                    Launch();
                }
            }

            return;
        }

        _resetRemaining -= (float)delta;
        if (_resetRemaining <= 0.0f)
        {
            ResetHazard();
        }
    }

    public void ResetHazard()
    {
        _launched = false;
        _launchRemaining = 0.0f;
        _resetRemaining = 0.0f;
        _rockBody.Freeze = true;
        _rockBody.CollisionLayer = 0;
        _rockBody.LinearVelocity = Vector3.Zero;
        _rockBody.AngularVelocity = Vector3.Zero;
        _rockBody.Position = Vector3.Zero;
        _rockBody.Rotation = Vector3.Zero;
        _rockCollision.SetDeferred("disabled", true);
    }

    private void OnTriggerBodyEntered(Node3D body)
    {
        if (_launched || _launchRemaining > 0.0f || ResolvePlayer(body) is null)
        {
            return;
        }

        _launchRemaining = LaunchDelay;
    }

    private void Launch()
    {
        _launched = true;
        _resetRemaining = ResetDelay;
        _rockBody.Freeze = false;
        _rockBody.CollisionLayer = 1;
        _rockCollision.SetDeferred("disabled", false);
        GD.Print("Falling rock launched");
    }

    private void OnRockBodyEntered(Node body)
    {
        PlayerController player = ResolvePlayer(body);
        if (player is null)
        {
            return;
        }

        Vector3 direction = _rockBody.LinearVelocity.Normalized();
        if (direction == Vector3.Zero)
        {
            direction = Vector3.Down;
        }

        player.ApplyHazardImpulse(direction * ImpactVelocity, ImpactStrength);
    }

    private static PlayerController ResolvePlayer(Node node)
    {
        return node as PlayerController ?? node.GetParent() as PlayerController;
    }
}
