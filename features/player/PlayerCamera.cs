using Godot;

namespace SteamMultiplayer.features.player;

public partial class PlayerCamera : Camera3D
{
	[Export] private Player _player;
	
	public override void _Ready()
	{
		// Only enable the camera for the local player
		if (!_player.IsMultiplayerAuthority())
		{
			// Disable this camera if it's not the local player's camera
			Current = false;
			// You can also hide it completely if desired
			// Visible = false;
		}
		else
		{
			// Make this camera the current one for the local player
			Current = true;
		}
	}
	
	public override void _PhysicsProcess(double delta)
	{
		// Only update position if this is the local player's camera
		if (_player.IsMultiplayerAuthority())
		{
			GlobalPosition = new(_player.GlobalPosition.X, GlobalPosition.Y, _player.GlobalPosition.Z + 4.5f);
		}
	}
}