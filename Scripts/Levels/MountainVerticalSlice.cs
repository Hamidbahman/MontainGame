using Godot;

public partial class MountainVerticalSlice : Node3D
{
	[Export] public NodePath Player1Path { get; set; } = new NodePath("");
	[Export] public NodePath Player2Path { get; set; } = new NodePath("");
	[Export] public NodePath RopePath { get; set; } = new NodePath("");
	[Export] public NodePath CheckpointStartPath { get; set; } = new NodePath("");
	[Export] public NodePath CheckpointMidPath { get; set; } = new NodePath("");
	[Export] public NodePath CheckpointHighPath { get; set; } = new NodePath("");
	[Export] public NodePath SafetyResetAreaPath { get; set; } = new NodePath("");
	[Export] public NodePath FinishAreaPath { get; set; } = new NodePath("");

	private PlayerController _player1 = null!;
	private PlayerController _player2 = null!;
	private PlayerRope _rope = null!;
	private Area3D _checkpointStart = null!;
	private Area3D _checkpointMid = null!;
	private Area3D _checkpointHigh = null!;
	private Area3D _safetyResetArea = null!;
	private Area3D _finishArea = null!;
	private Marker3D _activeRespawn = null!;
	private bool _summitReached;

	public override void _Ready()
	{
		_player1 = GetNode<PlayerController>(Player1Path);
		_player2 = GetNode<PlayerController>(Player2Path);
		_rope = GetNode<PlayerRope>(RopePath);
		_checkpointStart = GetNode<Area3D>(CheckpointStartPath);
		_checkpointMid = GetNode<Area3D>(CheckpointMidPath);
		_checkpointHigh = GetNode<Area3D>(CheckpointHighPath);
		_safetyResetArea = GetNode<Area3D>(SafetyResetAreaPath);
		_finishArea = GetNode<Area3D>(FinishAreaPath);
		_activeRespawn = GetNode<Marker3D>("Checkpoints/CheckpointStart/RespawnPoint");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (BothPlayersIn(_checkpointHigh))
		{
			SetCheckpoint(_checkpointHigh, "High");
		}
		else if (BothPlayersIn(_checkpointMid))
		{
			SetCheckpoint(_checkpointMid, "Mid");
		}

		if (AnyPlayerIn(_safetyResetArea))
		{
			ResetBothPlayers();
		}

		if (!_summitReached && BothPlayersIn(_finishArea))
		{
			_summitReached = true;
			GD.Print("SUMMIT REACHED!");
		}
	}

	private bool BothPlayersIn(Area3D area)
	{
		bool player1Present = false;
		bool player2Present = false;

		foreach (Node3D body in area.GetOverlappingBodies())
		{
			PlayerController player = body as PlayerController ?? body.GetParent() as PlayerController;
			player1Present |= player == _player1;
			player2Present |= player == _player2;
		}

		return player1Present && player2Present;
	}

	private bool AnyPlayerIn(Area3D area)
	{
		foreach (Node3D body in area.GetOverlappingBodies())
		{
			PlayerController player = body as PlayerController ?? body.GetParent() as PlayerController;
			if (player == _player1 || player == _player2)
			{
				return true;
			}
		}

		return false;
	}

	private void SetCheckpoint(Area3D checkpoint, string checkpointName)
	{
		Marker3D respawn = checkpoint.GetNode<Marker3D>("RespawnPoint");
		if (_activeRespawn == respawn)
		{
			return;
		}

		_activeRespawn = respawn;
		GD.Print($"Checkpoint reached: {checkpointName}");
	}

	private void ResetBothPlayers()
	{
		Vector3 center = _activeRespawn.GlobalPosition;
		_player1.ResetTo(center + Vector3.Left * 0.7f);
		_player2.ResetTo(center + Vector3.Right * 0.7f);
		_rope.ResetRopeState();

		foreach (Node node in GetTree().GetNodesInGroup("resettable_hazard"))
		{
			if (node is IResettableHazard hazard)
			{
				hazard.ResetHazard();
			}
		}

		foreach (Node node in GetTree().GetNodesInGroup("active_bomb"))
		{
			node.QueueFree();
		}
	}
}
