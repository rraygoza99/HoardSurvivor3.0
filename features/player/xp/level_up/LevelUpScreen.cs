using Godot;
using System;
using System.Collections.Generic;


public partial class LevelUpScreen : Control
{
	[Signal]
	public delegate void UpgradeChosenEventHandler(Upgrade upgrade);
	
	[Export] private PackedScene _upgradeCardScene;
	private HBoxContainer _cardContainer;
	private Label _waitingLabel;
	
	public override void _Ready(){
		_cardContainer = GetNode<HBoxContainer>("CenterContainer/HBoxContainer");
		
		// Create a waiting label (initially hidden)
		_waitingLabel = new Label();
		_waitingLabel.Text = "Waiting for other players to select upgrades...";
		_waitingLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_waitingLabel.ProcessMode = Node.ProcessModeEnum.WhenPaused;
		AddChild(_waitingLabel);
		_waitingLabel.Hide();
		
		// Connect to SharedXPManager signal to know when all players are ready
		if (SharedXPManager.Instance != null)
		{
			SharedXPManager.Instance.AllPlayersSelectedUpgrades += OnAllPlayersReady;
		}
	}
	
	public void DisplayUpgrades(List<Upgrade> upgrades)
	{
		foreach(Node child in _cardContainer.GetChildren())
		{
			child.QueueFree();
		}
		GD.Print(upgrades.Count);
		foreach(var upgrade in upgrades)
		{
			UpgradeCard card = _upgradeCardScene.Instantiate<UpgradeCard>();
			card.SetUpgrade(upgrade);
			card.UpgradeSelected += OnUpgradeSelected;
			// Ensure the card can process while the game is paused
			card.ProcessMode = Node.ProcessModeEnum.WhenPaused;
			_cardContainer.AddChild(card);
		}
		GD.Print("Time to show");
		Show();
		// Don't pause here - SharedXPManager handles pausing
	}
	private void OnUpgradeSelected(Upgrade upgrade)
	{
		// Hide the upgrade selection and show waiting message
		_cardContainer.Hide();
		_waitingLabel.Show();
		
		// Notify SharedXPManager that this player has selected an upgrade
		if (SharedXPManager.Instance != null)
		{
			SharedXPManager.Instance.OnPlayerSelectedUpgrade(Multiplayer.GetUniqueId());
		}
		
		EmitSignal(SignalName.UpgradeChosen, upgrade);
		// Don't resume here - SharedXPManager handles resuming when all players are ready
	}
	
	private void OnAllPlayersReady()
	{
		// Hide the entire level up screen when all players are ready
		Hide();
		_waitingLabel.Hide();
		_cardContainer.Show(); // Reset for next time
	}
}
