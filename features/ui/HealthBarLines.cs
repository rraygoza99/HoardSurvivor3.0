using Godot;

public partial class HealthBarLines : Control
{
    private ProgressBar _healthBar;

    public override void _Ready()
    {
    _healthBar = GetParent<ProgressBar>();
        // The _Draw method is called automatically, but we need to trigger it
        // whenever the size of the health bar changes.
        if (_healthBar != null)
        {
            _healthBar.Resized += () => QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (_healthBar == null)
        {
            return;
        }

        var barSize = _healthBar.Size;
        int segmentCount = (int)(_healthBar.MaxValue / 10);

        if (segmentCount <= 1) return;

        for (int i = 1; i < segmentCount; i++)
        {
            float x = (barSize.X / segmentCount) * i;
            DrawLine(new Vector2(x, 0), new Vector2(x, barSize.Y), new Color(0, 0, 0, 0.5f), 2.0f);
        }
    }
}