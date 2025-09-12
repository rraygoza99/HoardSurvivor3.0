using Godot;
using System;
using System.Collections.Generic;


public partial class LevelUpScreen : Control
{
	[Signal]
	public delegate void UpgradeChosenEventHandler(Upgrade upgrade);
	
	[Export] private PackedScene _upgradeCardScene;
	private HBoxContainer _cardContainer;
	
	public override void _Ready(){
		_cardContainer = GetNode<HBoxContainer>("CenterContainer/HBoxContainer");
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
			_cardContainer.AddChild(card);
		}
		GD.Print("Time to show");
		Show();
		GetTree().Paused= true;
	}
	private void OnUpgradeSelected(Upgrade upgrade)
	{
		Hide();
		GetTree().Paused = false;
		EmitSignal(SignalName.UpgradeChosen, upgrade);
	}
}
