using Godot;
using System;

public partial class Bug : Node2D
{
	[Export] public int legCount = 1;
	[Export] public int jointCount = 1;
	[Export] public float distance = 20;
	[Export] public float legSpeed = 0.1f;
	[Export] public float walkSpeed = 0.1f;
	private Vector2 pos = new(626, 324);
	private Leg[] legs;
	private Vector2 orientation;
	private Vector2 mousePos;

	public override void _Ready()
	{
		mousePos = pos;
		orientation = new Vector2(1, 1).Normalized();
		legs = new Leg[legCount];
		for (int i = 0; i < legs.Length; i++)
		{
			var side = i % 2 == 0 ? 1 : -1;
			
			legs[i] = new Leg(pos, orientation, side, jointCount+1, distance, legSpeed);
			legs[i].StartStep += StopTimers;
			legs[i].EndStep += StarTimers;
			legs[i].Recalculate();
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
		var dir = mousePos - pos;
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
		// move towards MouseClick
		if (Input.IsMouseButtonPressed(MouseButton.Left)) mousePos = GetGlobalMousePosition();
		var dir = mousePos - pos;
		var newPos = pos + dir.Normalized() * float.Min(dir.Length(), walkSpeed);
		
		// when moved, update ori
		if (pos != newPos)
		{
			orientation = (newPos - pos).Normalized();
			pos = newPos;
		}
		
		foreach (var leg in legs)
		{
			leg.pinPos = pos;
			leg.orientation = orientation;
			leg.Recalculate();
		}
		
		QueueRedraw();
	}
	
	public override void _Draw()
	{
		// orientation
		DrawLine(pos, pos+orientation*30, Colors.Blue, 1, true);
		
		var a = new Vector2(10, 10);
		var b = new Vector2(-10, 10);
		
		// // step X
		// for (var i=0; i<stepPoints.Length; i++)
		// {
		// 	var color = i>0? Colors.CornflowerBlue : Colors.DarkCyan;
		// 	DrawLine(stepPoints[i]-a/2, stepPoints[i]+a/2, color, 1, true);
		// 	DrawLine(stepPoints[i]+b/2, stepPoints[i]-b/2, color, 1, true);
		// }
		
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
	}

}
