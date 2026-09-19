using Godot;

[GlobalClass]
public partial class BombDefinition : ItemDefinition
{
    [Export] public PackedScene BombScene { get; set; }
    [Export] public float FuseDuration { get; set; } = 2.5f;
    [Export] public float ExplosionRadius { get; set; } = 4.0f;
    [Export] public float ExplosionForce { get; set; } = 12.0f;
    [Export] public float ExplosionUpwardForce { get; set; } = 7.0f;
    [Export] public float ThrowForwardSpeed { get; set; } = 5.0f;
    [Export] public float ThrowUpwardSpeed { get; set; } = 2.5f;

    public override void Use(PlayerController player)
    {
        if (BombScene is null)
        {
            return;
        }

        PhysicsBomb bomb = BombScene.Instantiate<PhysicsBomb>();
        player.GetTree().CurrentScene.AddChild(bomb);
        bomb.GlobalPosition = player.GetItemThrowPosition(0.9f, 1.0f);
        bomb.Deploy(player.GetItemThrowDirection(), this);
        GD.Print($"Player{player.PlayerId} deployed Bomb");
    }
}
