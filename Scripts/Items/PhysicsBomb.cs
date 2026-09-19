using Godot;

public partial class PhysicsBomb : RigidBody3D
{
    [Export] public PackedScene ExplosionVisualScene { get; set; }

    private MeshInstance3D _visual = null!;
    private BombDefinition _definition = null!;
    private float _fuseRemaining;
    private float _elapsed;
    private bool _deployed;

    public override void _Ready()
    {
        _visual = GetNode<MeshInstance3D>("MeshInstance3D");
        AddToGroup("active_bomb");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_deployed)
        {
            return;
        }

        float deltaTime = (float)delta;
        _elapsed += deltaTime;
        _fuseRemaining -= deltaTime;

        float urgency = 1.0f - Mathf.Clamp(_fuseRemaining / _definition.FuseDuration, 0.0f, 1.0f);
        float pulse = 1.0f + Mathf.Max(0.0f, Mathf.Sin(_elapsed * (5.0f + urgency * 20.0f))) * urgency * 0.2f;
        _visual.Scale = Vector3.One * pulse;

        if (_fuseRemaining <= 0.0f)
        {
            Explode();
        }
    }

    public void Deploy(Vector3 direction, BombDefinition definition)
    {
        _definition = definition;
        _fuseRemaining = definition.FuseDuration;
        _deployed = true;
        LinearVelocity = direction.Normalized() * definition.ThrowForwardSpeed + Vector3.Up * definition.ThrowUpwardSpeed;
        AngularVelocity = new Vector3(4.0f, 7.0f, 2.0f);
    }

    private void Explode()
    {
        foreach (Node node in GetTree().GetNodesInGroup("players"))
        {
            if (node is not PlayerController player)
            {
                continue;
            }

            Vector3 offset = player.GetRopeAnchorPosition(0.7f) - GlobalPosition;
            float distance = offset.Length();
            if (distance > _definition.ExplosionRadius)
            {
                continue;
            }

            float falloff = 1.0f - distance / _definition.ExplosionRadius;
            Vector3 direction = distance > 0.01f ? offset / distance : Vector3.Up;
            Vector3 velocityChange = direction * (_definition.ExplosionForce * falloff)
                + Vector3.Up * (_definition.ExplosionUpwardForce * falloff);
            float impactStrength = _definition.ExplosionForce * 10.0f * falloff;
            player.ApplyHazardImpulse(velocityChange, impactStrength);
        }

        foreach (Node node in GetTree().GetNodesInGroup("shared_camera_rig"))
        {
            if (node is SharedCamera camera)
            {
                float distance = camera.GlobalPosition.DistanceTo(GlobalPosition);
                camera.AddShake(Mathf.Clamp(1.0f - distance / (_definition.ExplosionRadius * 2.0f), 0.0f, 1.0f));
            }
        }

        if (ExplosionVisualScene is not null)
        {
            ExplosionVisual visual = ExplosionVisualScene.Instantiate<ExplosionVisual>();
            GetTree().CurrentScene.AddChild(visual);
            visual.GlobalPosition = GlobalPosition;
        }

        GD.Print("BOMB EXPLODED");
        QueueFree();
    }
}
