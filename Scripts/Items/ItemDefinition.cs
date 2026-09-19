using Godot;

[GlobalClass]
public partial class ItemDefinition : Resource
{
    [Export] public string ItemId { get; set; } = "item";
    [Export] public string DisplayName { get; set; } = "Item";

    public virtual void Use(PlayerController player)
    {
    }
}
