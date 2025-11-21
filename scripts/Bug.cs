using Godot;
using System;

public partial class Bug : Node2D
{
	[Export] public int legCount = 1;
	[Export] public int jointCount = 1;
	[Export] public float distance = 20;
	[Export] public float legSpeed = 0.1f;
	[Export] public float walkSpeed = 0.1f;
	public Vector2 pinPos;
	public Vector2 pos = new(626, 324);
	public Leg[] legs;
	public Vector2 orientation;
	private bool solo;

	public Bug()
	{
	}
	
	public Bug(Vector2 pinPos, Vector2 orientation, int legCount, int jointCount, float distance, float legSpeed, float walkSpeed)
	{
		
		pos = pinPos;
		this.orientation = orientation;
		this.legCount = legCount;
		this.jointCount = jointCount;
		this.distance = distance;
		this.legSpeed = legSpeed;
		this.walkSpeed = walkSpeed;
		
		Init();
		Recalculate();
	}

	public override void _Ready()
	{
		solo = true;
		orientation = new Vector2(1, 1).Normalized();
		Init();
	}

	private void Init()
	{
		pinPos = pos;
		legs = new Leg[legCount];
		for (int i = 0; i < legs.Length; i++)
		{
			var side = i % 2 == 0 ? 1 : -1;
			
			legs[i] = new Leg(pinPos, orientation, side, jointCount+1, distance, legSpeed);
			// legs[i].StartStep += StopTimers;
			// legs[i].EndStep += StarTimers;
		}

		// stepTimerL = new Timer();
		// stepTimerL.Timeout += () => TriggerStep(1);
		// AddChild(stepTimerL);
		// stepTimerR = new Timer();
		// stepTimerR.Timeout += () => TriggerStep(-1);
		// AddChild(stepTimerR);
		// StarTimers(0);
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
		var dir = pinPos - pos;
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
		if (!solo) return;
		
		// move towards MouseClick
		if (Input.IsMouseButtonPressed(MouseButton.Left)) pinPos = GetGlobalMousePosition();
		
		QueueRedraw();
	}

	public void Recalculate()
	{
		var dir = pinPos - pos;
		var newPos = pos + dir.Normalized() * float.Min(dir.Length(), walkSpeed);
		pos = newPos;
		
		
		foreach (var leg in legs)
		{
			leg.pinPos = pos;
			leg.orientation = orientation;
			leg.Recalculate();
		}
	}
	
	public override void _Draw()
	{
		// orientation
		DrawLine(pos, pos+orientation*30, Colors.Blue, 1, true);
		
		var a = new Vector2(10, 10);
		var b = new Vector2(-10, 10);
		
		foreach (var leg in legs)
		{
			// foot X
			DrawLine(leg.footPos-a, leg.footPos+a, Colors.DarkRed, 1, true);
			DrawLine(leg.footPos+b, leg.footPos-b, Colors.DarkRed, 1, true);
			
			// leg + reach
			for (var i = 1; i < leg.points.Count; i++)
			{
				DrawCircle(leg.points[0], distance*i ,new Color(.5f, .5f, .5f, .5f), false);
				DrawCircle(leg.points[i], distance ,new Color(.5f, .5f, .5f, .3f), false);
				DrawLine(leg.points[i-1], leg.points[i], Colors.Black, 5, true);
			}
			// joint
			for (var i = 1; i < leg.points.Count; i++)
			{
				DrawCircle(leg.points[i], 7, Colors.SteelBlue, true);
			}
		}
		
		// pin
		DrawCircle(pos, 10, Colors.Blue, true);
		
		// foreach (var bug in bugs)
		// {
		// 	// orientation
		// 	DrawLine(bug.pos, bug.pinPos+bug.orientation*30, Colors.DimGray, 1, true);
		// 	
		// 	var a = new Vector2(10, 10);
		// 	var b = new Vector2(-10, 10);
		// 	
		// 	foreach (var leg in bug.legs)
		// 	{
		// 		// foot X
		// 		DrawLine(leg.footPos-a, leg.footPos+a, Colors.DarkRed, 1, true);
		// 		DrawLine(leg.footPos+b, leg.footPos-b, Colors.DarkRed, 1, true);
		// 	
		// 		// leg
		// 		for (var i = 1; i < leg.points.Count; i++)
		// 		{
		// 			DrawLine(leg.points[i-1], leg.points[i], Colors.Black, 3, true);
		// 		}
		// 		// joint
		// 		for (var i = 1; i < leg.points.Count; i++)
		// 		{
		// 			DrawCircle(leg.points[i], 7, Colors.SteelBlue, true);
		// 		}
		// 	}
		//
		// 	// pin
		// 	DrawCircle(bug.pos, 10, Colors.DimGray, true);
		// 	DrawCircle(bug.pos, jointDistance * jointCount, new Color(.5f, .5f, .5f, .3f), false);
		// }
	}

}
