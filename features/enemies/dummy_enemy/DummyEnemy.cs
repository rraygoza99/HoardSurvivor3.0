using Godot;
using System;

public partial class DummyEnemy : CharacterBody3D
{
    public override void _Ready()
    {
        GD.Print("DummyEnemy is ready.");
        var isMultiplayerAuthority = IsMultiplayerAuthority();
        SetProcess(isMultiplayerAuthority);
        SetPhysicsProcess(isMultiplayerAuthority);
        if (!isMultiplayerAuthority)
        {
            return;
        }
        var main = GetTree().Root.GetNode<Node>("Main");
        main.Connect("enemy_teleport", new Callable(this, MethodName.OnEnemyTeleport));
    }
    public override void _Process(double delta)
    {
        RotateY((float)(delta * 0.5));
    }
    public override void _PhysicsProcess(double delta)
    {
        // Simple back-and-forth movement
        var speed = 2.0f;
        var direction = new Vector3(Mathf.Sin((float)Time.GetTicksMsec() / 1000.0f), 0, 0);
        Velocity = direction * speed;
        MoveAndSlide();
    }
    private void OnEnemyTeleport(Vector3 newPosition)
    {
        GD.Print("Teleporting enemy.. - ", Name);
        GD.Print("New pos: ", newPosition);
        GlobalPosition = newPosition;
    }
}
