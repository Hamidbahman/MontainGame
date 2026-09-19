using Godot;

[GlobalClass]
public partial class TestPickupDefinition : ItemDefinition
{
    public override void Use(PlayerController player)
    {
        GD.Print($"Player{player.PlayerId} used {DisplayName}");
    }
}
