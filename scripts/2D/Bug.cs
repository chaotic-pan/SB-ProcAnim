using Godot;
using System;

public partial class Bug : Node2D
{
	[Export] public int LegCount = 1;
	[Export] public float DistanceUp = 20;
	[Export] public float DistanceLow = 20;
	[Export] private float legSpeed = 0.1f;
	[Export] public float WalkSpeed = 0.1f;
	[Export] public float StepInterval = 2;
	[Export] public bool DrawRanges = true;
	[Export] public bool DrawAsMesh = true;
	[Export] public bool KneeBackwards;
	[Export] private float stepWidth;
	[Export] private float stepDistance;
	private Vector2 lookPos;
	private Vector2 lastPos;
	private Vector2 pos;
	private Leg[] legs;
	private Vector2 orientation;
	private bool solo = true;

	public Bug()
	{
	}
	
	public Bug(Vector2 lookPos, Vector2 pos, bool kneeBackwards, int legCount, float distanceUp, float distanceLow, float legSpeed, 
		float walkSpeed, float stepInterval, float stepWidth, bool drawRanges, bool drawAsMesh)
	{
		this.lookPos = lookPos;
		this.pos = pos;
		orientation = (lookPos - pos).Normalized();
		KneeBackwards = kneeBackwards;
		LegCount = legCount;
		DistanceUp = distanceUp;
		DistanceLow = distanceLow;
		this.legSpeed = legSpeed;
		WalkSpeed = walkSpeed;
		StepInterval = stepInterval;
		this.stepWidth = stepWidth;
		stepDistance = 1;
		DrawRanges = drawRanges;
		DrawAsMesh = drawAsMesh;
		solo = false;
	}

	public override void _Ready()
	{
		if (solo)
		{
			pos = new(626, 324);
			orientation = new Vector2(-1, -1).Normalized();
			lookPos = pos - orientation;
			Init();
		}
	}

	public void Init()
	{
		legs = new Leg[LegCount];
		for (int i = 0; i < legs.Length; i++)
		{
			var side = i % 2 == 0 ? 1 : -1;
			
			legs[i] = new Leg(lookPos, orientation, KneeBackwards, side, DistanceUp, DistanceLow, 
				legSpeed, stepWidth, stepDistance, DrawRanges, DrawAsMesh);
			AddChild(legs[i]);
			// legs[i].StartStep += StopTimers;
			// legs[i].EndStep += StarTimers;
		}

		stepTimer = new Timer();
		stepTimer.Timeout += () => TriggerStep();
		AddChild(stepTimer);
		stepTimer.Start(StepInterval);
	}

	private Timer stepTimer;

	private void StarTimers()
	{
		// stepTimer.SetPaused(false);
		// stepTimer.Start(side == 1 ? stepTimer : stepTimer/2);
	}

	private void StopTimers()
	{
		// stepTimerL.SetPaused(true);
		// stepTimerR.SetPaused(true);
	}

	private void TriggerStep()
	{
		// check which foot is further out -> move that

		var disL = legs[0].getFootDistance();
		var disR = legs[1].getFootDistance();
		var i = disL > disR ? 0 : 1;
		
		// when moved
		if (pos != lastPos) legs[i].triggerStep();
		else legs[i].triggerRest();
		
		stepTimer.Start(StepInterval);
	}
	
	public override void _Process(double delta)
	{
		if (solo)
		{
			// move towards MouseClick
			if (Input.IsMouseButtonPressed(MouseButton.Left)) lookPos = GetGlobalMousePosition();
            		
			var dir = lookPos - pos;
			var newPos = pos + dir.Normalized() * float.Min(dir.Length(), WalkSpeed);
			pos = newPos;
		}
		
		if (lookPos != pos) orientation = (lookPos - pos).Normalized();
		
		foreach (var leg in legs)
		{
			leg.Refresh(pos, orientation);
		}
		
		// GetChild<Label>(0).Text = Math.Round((decimal)stepTimer.TimeLeft,1).ToString();
		// GetChild<Label>(1).Text = Math.Round((decimal)stepTimerR.TimeLeft, 1).ToString();
		
		QueueRedraw();
	}
	
	public override void _Draw()
	{
		if (!DrawRanges) return;
		// orientation
		DrawLine(pos, pos+orientation*30, Colors.Blue, 1, true);
		
		// pin
		DrawCircle(pos, 10, Colors.Blue, true);
	}

	public void Refresh(Vector2 bugPos, Vector2 orientation)
	{
		lastPos = pos;
		pos = bugPos;
		lookPos = orientation;

	}
}
