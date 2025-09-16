using Godot;
using System;

public partial class PlayerUI : Control
{
    private TextureProgressBar _healthBar;
    private ProgressBar _xpBar;
    private Label _levelLabel;
    private Label _healthLabel;

    public override void _Ready()
    {
        _healthBar = GetNode<TextureProgressBar>("HealthBar");
        _xpBar = GetNode<ProgressBar>("XPBar");
        _levelLabel = GetNode<Label>("LevelLabel");
        _healthLabel = GetNode<Label>("HealthBar/HealthLabel");
    }

    public void SetHealth(float health, float maxHealth)
    {
        _healthBar.MaxValue = maxHealth;
        _healthBar.Value = health;
        _healthLabel.Text = $"{health}/{maxHealth}";
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
