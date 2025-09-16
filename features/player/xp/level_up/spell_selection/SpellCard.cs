using Godot;
using HoardSurvivor3._0.Features.Spells.Base;

public partial class SpellCard : PanelContainer
{
    [Signal]
    public delegate void SpellSelectedEventHandler(int spellType);

    private ISpell _spell;
    private Label _nameLabel;
    private Label _descriptionLabel;
    private Button _selectButton;

    public override void _Ready()
    {
        _nameLabel = GetNode<Label>("VBoxContainer/NameLabel");
        _descriptionLabel = GetNode<Label>("VBoxContainer/DescriptionLabel");
        _selectButton = GetNode<Button>("VBoxContainer/SelectButton");

        _selectButton.Pressed += OnSelectButtonPressed;
    }

    public void SetSpell(ISpell spell)
    {
        _spell = spell;
        _nameLabel.Text = spell.Name;
        _descriptionLabel.Text = spell.Description;
    }

    private void OnSelectButtonPressed()
    {
        EmitSignal(SignalName.SpellSelected, (int)_spell.SpellType);
    }
}