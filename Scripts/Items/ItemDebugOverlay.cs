using Godot;

public partial class ItemDebugOverlay : CanvasLayer
{
    [Export] public NodePath Player1Path { get; set; } = new NodePath("");
    [Export] public NodePath Player2Path { get; set; } = new NodePath("");

    private PlayerController _player1 = null!;
    private PlayerController _player2 = null!;
    private Label _label = null!;

    public override void _Ready()
    {
        _player1 = GetNode<PlayerController>(Player1Path);
        _player2 = GetNode<PlayerController>(Player2Path);
        _label = GetNode<Label>("MarginContainer/Label");
    }

    public override void _Process(double delta)
    {
        _label.Text = $"Player1 Item: {_player1.CurrentItemName}\nPlayer2 Item: {_player2.CurrentItemName}";
    }
}
