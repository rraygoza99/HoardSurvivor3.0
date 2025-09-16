using Godot;
using System;

public partial class PlayerUI : Control
{
    private ProgressBar _healthBar;
    private ProgressBar _xpBar;
    private Label _levelLabel;

    public override void _Ready()
    {
        _healthBar = GetNode<ProgressBar>("HealthBar");
        _xpBar = GetNode<ProgressBar>("XPBar");
        _levelLabel = GetNode<Label>("LevelLabel");
    }

    public void SetHealth(float health, float maxHealth)
    {
        _healthBar.Value = (health / maxHealth) * 100;
    }

    public void SetXP(float xp, float maxXp)
    {
        _xpBar.Value = (xp / maxXp) * 100;
    }

    public void SetLevel(int level)
    {
        _levelLabel.Text = $"Level: {level}";
    }
}
