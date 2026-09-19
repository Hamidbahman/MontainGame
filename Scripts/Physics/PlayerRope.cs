using Godot;

public partial class PlayerRope : Node3D
{
    [Export] public NodePath Player1Path { get; set; } = new NodePath("");
    [Export] public NodePath Player2Path { get; set; } = new NodePath("");
    [Export] public float RopeLength { get; set; } = 6.0f;
    [Export] public float RopeStiffness { get; set; } = 35.0f;
    [Export] public float RopeDamping { get; set; } = 8.0f;
    [Export] public float MaxCorrectionSpeed { get; set; } = 12.0f;
    [Export] public float AnchorHeight { get; set; } = 0.7f;
    [Export] public float MaxSlackSag { get; set; } = 0.8f;

    [Export] public float CurrentDistance { get; private set; }
    [Export] public float CurrentTension { get; private set; }
    [Export] public bool IsTaut { get; private set; }

    private PlayerController _player1 = null!;
    private PlayerController _player2 = null!;
    private MeshInstance3D _ropeVisual = null!;
    private ImmediateMesh _ropeMesh = null!;
    private StandardMaterial3D _ropeMaterial = null!;

    public override void _Ready()
    {
        _player1 = GetNodeOrNull<PlayerController>(Player1Path);
        _player2 = GetNodeOrNull<PlayerController>(Player2Path);
        _ropeVisual = GetNode<MeshInstance3D>("RopeVisual");

        _ropeMesh = new ImmediateMesh();
        _ropeMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.95f, 0.72f, 0.12f),
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
        };
        _ropeVisual.Mesh = _ropeMesh;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_player1 is null || _player2 is null || RopeLength <= 0.0f)
        {
            SetTaut(false);
            CurrentDistance = 0.0f;
            CurrentTension = 0.0f;
            ClearPlayerLoads();
            return;
        }

        Vector3 start = GetAnchorPosition(_player1);
        Vector3 end = GetAnchorPosition(_player2);
        Vector3 offset = end - start;
        CurrentDistance = offset.Length();

        if (CurrentDistance < 0.001f || CurrentDistance <= RopeLength)
        {
            CurrentTension = 0.0f;
            ClearPlayerLoads();
            SetTaut(false);
            return;
        }

        Vector3 direction = offset / CurrentDistance;
        float stretch = CurrentDistance - RopeLength;
        float separatingSpeed = (_player2.GetRopeVelocity() - _player1.GetRopeVelocity()).Dot(direction);
        CurrentTension = Mathf.Max(0.0f, stretch * RopeStiffness + Mathf.Max(0.0f, separatingSpeed) * RopeDamping);

        float correctionSpeed = Mathf.Min(CurrentTension * (float)delta, MaxCorrectionSpeed);
        Vector3 velocityChange = direction * correctionSpeed;
        _player1.ApplyRopeTension(velocityChange, CurrentTension);
        _player2.ApplyRopeTension(-velocityChange, CurrentTension);
        SetTaut(true);
    }

    public override void _Process(double delta)
    {
        if (_player1 is null || _player2 is null || _ropeMesh is null)
        {
            return;
        }

        DrawRope(GetAnchorPosition(_player1), GetAnchorPosition(_player2));
    }

    private Vector3 GetAnchorPosition(PlayerController player)
    {
        return player.GetRopeAnchorPosition(AnchorHeight);
    }

    private void DrawRope(Vector3 start, Vector3 end)
    {
        const int segments = 12;
        float sag = IsTaut ? 0.0f : Mathf.Min(MaxSlackSag, Mathf.Max(0.0f, RopeLength - CurrentDistance) * 0.15f);

        _ropeMesh.ClearSurfaces();
        _ropeMesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip, _ropeMaterial);

        for (int index = 0; index <= segments; index++)
        {
            float t = (float)index / segments;
            Vector3 point = start.Lerp(end, t) - Vector3.Up * sag * 4.0f * t * (1.0f - t);
            _ropeMesh.SurfaceAddVertex(ToLocal(point));
        }

        _ropeMesh.SurfaceEnd();
    }

    private void SetTaut(bool taut)
    {
        if (IsTaut == taut)
        {
            return;
        }

        IsTaut = taut;
        GD.Print(taut ? "Rope became taut" : "Rope became slack");
    }

    private void ClearPlayerLoads()
    {
        if (_player1 is not null)
        {
            _player1.ApplyRopeTension(Vector3.Zero, 0.0f);
        }

        if (_player2 is not null)
        {
            _player2.ApplyRopeTension(Vector3.Zero, 0.0f);
        }
    }

    public void ResetRopeState()
    {
        CurrentDistance = 0.0f;
        CurrentTension = 0.0f;
        SetTaut(false);
        ClearPlayerLoads();
    }
}
