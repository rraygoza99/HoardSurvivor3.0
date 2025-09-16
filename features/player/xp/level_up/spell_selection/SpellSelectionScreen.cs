using Godot;
using System.Collections.Generic;
using HoardSurvivor3._0.Core.Enums;
using HoardSurvivor3._0.Features.Spells.Base;

public partial class SpellSelectionScreen : Control
{
    [Signal]
    public delegate void SpellChosenEventHandler(int spellType);

    [Export] private PackedScene _spellCardScene;
    private HBoxContainer _cardContainer;
    private Label _waitingLabel;

    public override void _Ready()
    {
        _cardContainer = GetNode<HBoxContainer>("CenterContainer/HBoxContainer");

        _waitingLabel = new Label();
        _waitingLabel.Text = "Waiting for other players to select spells...";
        _waitingLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _waitingLabel.ProcessMode = ProcessModeEnum.WhenPaused;
        AddChild(_waitingLabel);
        _waitingLabel.Hide();

        if (SharedXPManager.Instance != null)
        {
            SharedXPManager.Instance.AllPlayersSelectedUpgrades += OnAllPlayersReady;
        }
    }

    public void DisplaySpells(List<ISpell> spells)
    {
        foreach (Node child in _cardContainer.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var spell in spells)
        {
            SpellCard card = _spellCardScene.Instantiate<SpellCard>();
            card.SetSpell(spell);
            card.SpellSelected += OnSpellSelected;
            card.ProcessMode = ProcessModeEnum.WhenPaused;
            _cardContainer.AddChild(card);
        }
        Show();
    }

    private void OnSpellSelected(int spellType)
    {
        _cardContainer.Hide();
        _waitingLabel.Show();

        if (SharedXPManager.Instance != null)
        {
            SharedXPManager.Instance.OnPlayerSelectedUpgrade(Multiplayer.GetUniqueId());
        }

        EmitSignal(SignalName.SpellChosen, spellType);
    }

    private void OnAllPlayersReady()
    {
        Hide();
        _waitingLabel.Hide();
        _cardContainer.Show();
    }
}