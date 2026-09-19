using Godot;
using System.Collections.Generic;

public partial class IceSurface : Area3D
{
    [Export(PropertyHint.Range, "0.05,1.0,0.05")] public float AccelerationMultiplier { get; set; } = 0.35f;
    [Export(PropertyHint.Range, "0.05,1.0,0.05")] public float DecelerationMultiplier { get; set; } = 0.12f;

    private readonly HashSet<PlayerController> _affectedPlayers = new();

    public override void _PhysicsProcess(double delta)
    {
        HashSet<PlayerController> currentPlayers = new();
        foreach (Node3D body in GetOverlappingBodies())
        {
            PlayerController player = ResolvePlayer(body);
            if (player is null)
            {
                continue;
            }

            currentPlayers.Add(player);
            player.SetSurfaceTraction(this, AccelerationMultiplier, DecelerationMultiplier);
        }

        foreach (PlayerController player in _affectedPlayers)
        {
            if (!currentPlayers.Contains(player))
            {
                player.ClearSurfaceTraction(this);
            }
        }

        _affectedPlayers.Clear();
        _affectedPlayers.UnionWith(currentPlayers);
    }

    private static PlayerController ResolvePlayer(Node node)
    {
        return node as PlayerController ?? node.GetParent() as PlayerController;
    }
}
