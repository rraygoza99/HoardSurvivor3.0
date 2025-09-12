using Godot;
using System;

public partial class XpOrb : Area3D
{
	public int XpAmount { get; set; } = 1;

	[Export] private float _speed = 15.0f;
	[Export] private float _repelTime = 0.25f;
	[Export] private float _repelSpeed = 10.0f;

	private Node3D _targetPlayer;

	private enum State { Idle, Repelling, Seeking }
	private State _currentState = State.Idle;

	private float _easeTimer = 0.0f;

	public void Combine(int amount)
	{
		XpAmount += amount;
	}

	public void SetInitialValue(int amount)
	{
		XpAmount = amount;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_targetPlayer == null)
			return;

		switch (_currentState)
		{
			case State.Repelling:
				_easeTimer += (float)delta;
				float repelProgress = _easeTimer / _repelTime;
				float repelSpeedCurrent = Mathf.Lerp(_repelSpeed, 0, repelProgress);
				Vector3 repelDir = (GlobalPosition - _targetPlayer.GlobalPosition).Normalized();
				GlobalPosition += repelDir * repelSpeedCurrent * (float)delta;

				if (_easeTimer >= _repelTime)
				{
					_currentState = State.Seeking;
					_easeTimer = 0f;
				}
				break;

			case State.Seeking:
				_easeTimer += (float)delta;
				float seekProgress = _easeTimer / _repelTime;
				float seekSpeedCurrent = Mathf.Lerp(0, _speed, seekProgress);
				Vector3 seekDir = (_targetPlayer.GlobalPosition - GlobalPosition).Normalized();
				GlobalPosition += seekDir * seekSpeedCurrent * (float)delta;
				break;

			case State.Idle:
			default:
				break;
		}
	}

	public void StartSeeking(Node3D player)
	{
		if (_currentState == State.Repelling || _currentState == State.Seeking)
		{
			return;
		}
		
		
		_targetPlayer = player;
		_currentState = State.Repelling;
		_easeTimer = 0f;
	}
}
