using Godot;
using System;

public partial class Bug : Node2D
{
	[Export] public int legCount = 1;
	[Export] public float distance = 20;
	[Export] public float legSpeed = 0.1f;
	[Export] public float walkSpeed = 0.1f;
	[Export] public bool drawRanges = true;
	public Vector2 lookPos;
	public Vector2 pos = new(626, 324);
	public Leg[] legs;
	public Vector2 orientation;
	private bool solo = true;

	public Bug()
	{
	}
	
	public Bug(Vector2 lookPos, Vector2 pos, int legCount, float distance, float legSpeed, 
		float walkSpeed, bool drawRanges)
	{
		this.lookPos = lookPos;
		this.pos = pos;
		orientation = (lookPos - pos).Normalized();
		this.legCount = legCount;
		this.distance = distance;
		this.legSpeed = legSpeed;
		this.walkSpeed = walkSpeed;
		this.drawRanges = drawRanges;
		solo = false;
		
		Init();
	}

	public override void _Ready()
	{
		if (solo)
		{
			orientation = new Vector2(1, 1).Normalized();
			Init();
		}
	}

	private void Init()
	{
		legs = new Leg[legCount];
		for (int i = 0; i < legs.Length; i++)
		{
			var side = i % 2 == 0 ? 1 : -1;
			
			legs[i] = new Leg(lookPos, orientation, side, distance, distance, legSpeed, drawRanges);
			AddChild(legs[i]);
			legs[i].StartStep += StopTimers;
			legs[i].EndStep += StarTimers;
		}

		stepTimerL = new Timer();
		stepTimerL.Timeout += () => TriggerStep(1);
		AddChild(stepTimerL);
		stepTimerR = new Timer();
		stepTimerR.Timeout += () => TriggerStep(-1);
		AddChild(stepTimerR);
		StarTimers(0);
	}

	private Timer stepTimerL;
	private Timer stepTimerR;

	private void StarTimers(int side)
	{
		stepTimerL.SetPaused(false);
		stepTimerL.Start(side*0.25 + 0.75);
		stepTimerR.SetPaused(false);
		stepTimerR.Start(side*-0.25 + 0.75);
	}

	private void StopTimers(int side)
	{
		stepTimerL.SetPaused(true);
		stepTimerR.SetPaused(true);
	}

	private void TriggerStep(int side)
	{
		var dir = lookPos - pos;
		var newPos = pos + dir.Normalized() * float.Min(dir.Length(), walkSpeed);

		int i = (int)(side * -0.5 + 0.5);
		
		// when moved, update ori
		if (pos != newPos)
		{
			legs[i].triggerStep();
		}
		legs[i].triggerRest();
	}
	
	public override void _Process(double delta)
	{
		if (solo)
		{
			// move towards MouseClick
			if (Input.IsMouseButtonPressed(MouseButton.Left)) lookPos = GetGlobalMousePosition();
            		
			var dir = lookPos - pos;
			var newPos = pos + dir.Normalized() * float.Min(dir.Length(), walkSpeed);
			pos = newPos;
		}
		
		if (lookPos != pos) orientation = (lookPos - pos).Normalized();
		
		foreach (var leg in legs)
		{
			leg.Refresh(pos, orientation);
		}
		
		QueueRedraw();
	}
	
	public override void _Draw()
	{
		// orientation
		DrawLine(pos, pos+orientation*30, Colors.Blue, 1, true);
		
		// pin
		DrawCircle(pos, 10, Colors.Blue, true);
	}

}
