using Godot;
using System;

public partial class UpgradeCard : PanelContainer
{
	[Signal]
	public delegate void UpgradeSelectedEventHandler(Upgrade upgrade);
	
	private Label _nameLabel;
	private Label _descriptionLabel;
	private Button _button;
	private Upgrade _upgrade;
	
	public override void _Ready(){
		
		_button = GetNode<Button>("Button");
		_button.Pressed += OnButtonPressed;
		
		// Set minimum height to make the cards taller
		CustomMinimumSize = new Vector2(CustomMinimumSize.X, 150); // Set height to 150 pixels
	}
	public void SetUpgrade(Upgrade upgrade)
	{
		_nameLabel = GetNode<Label>("Button/VBoxContainer/NameLabel");
		_descriptionLabel = GetNode<Label>("Button/VBoxContainer/DescriptionLabel");
		_upgrade = upgrade;
		_nameLabel.Text = upgrade.Name;
		_descriptionLabel.Text = upgrade.Description;
		var styleBox = new StyleBoxFlat { BgColor = GetRarityColor(upgrade.Rarity) };
		AddThemeStyleboxOverride("panel", styleBox);
		
		// Ensure the button expands to fill the card height
		if (_button != null)
		{
			_button.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		}
		
		// Format the name label (title)
		if (_nameLabel != null)
		{
			_nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
			_nameLabel.VerticalAlignment = VerticalAlignment.Center;
			_nameLabel.AutowrapMode = TextServer.AutowrapMode.Off; // Disable autowrap to prevent weird breaks
			_nameLabel.ClipContents = true;
			_nameLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			// Make title text smaller and bold
			_nameLabel.AddThemeFontSizeOverride("font_size", 16);
		}
		
		// Format the description label
		if (_descriptionLabel != null)
		{
			_descriptionLabel.HorizontalAlignment = HorizontalAlignment.Center;
			_descriptionLabel.VerticalAlignment = VerticalAlignment.Center;
			_descriptionLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			_descriptionLabel.ClipContents = true;
			_descriptionLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			_descriptionLabel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
			// Make description text smaller
			_descriptionLabel.AddThemeFontSizeOverride("font_size", 12);
		}
	}
	private void OnButtonPressed()
	{
		EmitSignal(SignalName.UpgradeSelected, _upgrade);
	}

	private Color GetRarityColor(Rarity rarity)
	{
		switch (rarity)
		{
			case Rarity.Common:
				return new Color("#a8a8a8");
			case Rarity.Rare:
				return new Color("#4d84ff");
	 		  case Rarity.Legendary:
				return new Color("#ffc54d");
			default:
				return new Color("#ffffff");
		}
	}
}
