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
	Vector2[] stepPoints;
	private Vector2 orientation;
	private Vector2 mousePos;

	public override void _Ready()
	{
		mousePos = pos;
		legs = new Leg[legCount];
		stepPoints = new Vector2[legCount];
		for (int i = 0; i < legs.Length; i++)
		{
			var side = i % 2 == 0 ? new Vector2(1, 0) : new Vector2(-1, 0);
			legs[i] = new Leg(pos, side, jointCount+1, distance, legSpeed);
			legs[i].Recalculate();
		}
		orientation = new Vector2(0, -1);
		StepCalc(40);
	}
	
	double restTimer = 2;
	double stepTimerL = 0.5;
	double stepTimerR = 0;
	
	public override void _Process(double delta)
	{
		stepTimerL -= delta;
		stepTimerR -= delta;
		// move towards MouseClick
		if (Input.IsMouseButtonPressed(MouseButton.Left)) mousePos = GetGlobalMousePosition();
		var dir = mousePos - pos;
		var newPos = pos + dir.Normalized() * float.Min(dir.Length(), walkSpeed);
		
		// when moved update ori and stepPoints
		if (pos != newPos)
		{
			restTimer = 2;
			orientation = (newPos - pos).Normalized();
			pos = newPos;
			
		}
		else
		{
			// start timer for manual step
			restTimer -= delta;
		}

		if (restTimer <= 0)
		{
			stepTimerL =.5 ;
			stepTimerR = 0;
			StepCalc(100);
			for (int i = 0; i < legs.Length; i++)
			{
				var restStep = (stepPoints[i] - pos);
				legs[i].triggerStep(pos + restStep*0.3f);
			}
		}
		else {
			StepCalc(40);
			if (stepTimerL <= 0)
			{
				stepTimerL = 1;
				legs[0].stepPos = stepPoints[0];
			}
			else if (stepTimerR <= 0)
			{
				stepTimerR = 1;
				legs[1].stepPos = stepPoints[1];
			}
			
			foreach (var leg in legs)
			{
				leg.pinPos = pos;
				leg.Recalculate();
			}
			
		}

		
		GetChild<Label>(0).Text = Math.Round(stepTimerL,2).ToString();
		GetChild<Label>(1).Text = Math.Round(stepTimerR,2).ToString();
		QueueRedraw();
	}

	void StepCalc(float offsetAngle)
	{
		for (int i = 0; i < legs.Length; i++)
		{
			// set point +/- 30° from orientation
			var side = i % 2 == 0 ? 1 : -1;
			var angle= orientation.Angle() + side*(Math.PI / 180 * offsetAngle);
			
			// adjust for Pi wrap
			if (angle > Math.PI) angle -= 2 * Math.PI;
			if (angle < -Math.PI) angle += 2 * Math.PI;
			
			var x = (float)(distance*jointCount * Math.Cos(angle));
			var y = (float)(distance*jointCount * Math.Sin(angle));
			stepPoints[i] = pos + new Vector2(x,y);
		}
		
	}
	
	public override void _Draw()
	{
		// orientation
		DrawLine(pos, pos+orientation*30, Colors.Blue, 1, true);
		
		var a = new Vector2(10, 10);
		var b = new Vector2(-10, 10);
		
		// step X
		for (var i=0; i<stepPoints.Length; i++)
		{
			var color = i>0? Colors.CornflowerBlue : Colors.DarkCyan;
			DrawLine(stepPoints[i]-a/2, stepPoints[i]+a/2, color, 1, true);
			DrawLine(stepPoints[i]+b/2, stepPoints[i]-b/2, color, 1, true);
		}
		
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
