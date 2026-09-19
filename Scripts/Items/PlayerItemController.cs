using Godot;

public partial class PlayerItemController : Node
{
    public bool HasItem => CurrentItem is not null;
    public ItemDefinition CurrentItem { get; private set; }

    private PlayerController _player = null!;

    public override void _Ready()
    {
        _player = GetParent<PlayerController>();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_player.IsKnockedDown && Input.IsActionJustPressed($"p{_player.PlayerId}_use_item"))
        {
            TryUseItem();
        }
    }

    public bool TryPickUp(ItemDefinition item)
    {
        if (item is null || HasItem)
        {
            return false;
        }

        CurrentItem = item;
        GD.Print($"Player{_player.PlayerId} picked up {item.DisplayName}");
        return true;
    }

    private void TryUseItem()
    {
        if (CurrentItem is null)
        {
            return;
        }

        ItemDefinition item = CurrentItem;
        item.Use(_player);
        CurrentItem = null;
    }
}
