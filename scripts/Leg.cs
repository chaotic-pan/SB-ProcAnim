using Godot;
using System;
using System.Collections.Generic;

public partial class Leg : Node2D
{
	public Dictionary<int, Vector2> points = new();
	public Vector2 pinPos = new(626, 324);
	public Vector2 footPos;
	public Vector2 orientation;
	private int side = 1;
	[Export] public int count = 5;
	[Export] public float distance = 20;
	[Export] public float speed = 5;
	private bool solo;
	[Signal] public delegate void StartStepEventHandler(int side);
	[Signal] public delegate void EndStepEventHandler(int side);
	
	public Leg()
	{
	}

	public Leg(Vector2 pinPos, Vector2 orientation, int side, int count, float distance, float speed)
	{
		this.pinPos = pinPos;
		this.orientation = orientation;
		this.side = side;
		this.count = count;
		this.distance = distance;
		this.speed = speed;
		
		var angle= orientation.Angle() + side*(Math.PI/2);
		// adjust for Pi wrap
		if (angle > Math.PI) angle -= 2 * Math.PI;
		if (angle < -Math.PI) angle += 2 * Math.PI;
		var direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
		
		Init(direction);
		Recalculate();
	}

	public override void _Ready()
	{
		count++;
		solo = true;
		Init(new Vector2(1,0));
	}

	private void Init(Vector2 direction)
	{
		var pos = pinPos;
		for (int i = 0; i < count; i++)
		{
			points.Add(i, pos);
			pos += direction*distance;
		}

		footPos = pinPos + direction*distance*1.5f;
	}
	
	public override void _Process(double delta)
	{
		if (!solo) return;
		if (Input.IsKeyPressed(Key.S)) pinPos.Y++;
		if (Input.IsKeyPressed(Key.W)) pinPos.Y--;
		if (Input.IsKeyPressed(Key.D)) pinPos.X++;
		if (Input.IsKeyPressed(Key.A)) pinPos.X--;

		if (Input.IsMouseButtonPressed(MouseButton.Left))
		{
			footPos = GetGlobalMousePosition();
			step = false;
			
		}
		
		Recalculate();
		QueueRedraw();

	}

	public void triggerStep()
	{
		footPos = pinPos + StepCalc(30);
		Recalculate();
	}

	
	public void triggerRest()
	{
		footPos = pinPos + StepCalc(90).Normalized() * (distance*0.75f);
		step = false;
		Recalculate();
	}
	
	private Vector2 StepCalc(float offsetAngle)
	{
		
		// set point +/- 30° from orientation
		var angle= orientation.Angle() + side*(Math.PI / 180 * offsetAngle);
			
		// adjust for Pi wrap
		if (angle > Math.PI) angle -= 2 * Math.PI;
		if (angle < -Math.PI) angle += 2 * Math.PI;
			
		var x = (float)(distance*(count-1) * Math.Cos(angle));
		var y = (float)(distance*(count-1) * Math.Sin(angle));
		return new Vector2(x,y);
	}

	private bool step = false;
	
	public void Recalculate()
	{
		points[0] = pinPos;
		
		// get new footPos when outta range 
		if ((footPos-pinPos).Length() > distance*(count-1))
		{
			footPos = pinPos + StepCalc(30);
			EmitSignal(SignalName.StartStep, side);
			step = true;
		}
		// + keep updating til foot reached it 
		if (step && Math.Abs((points[count - 1]-footPos).Length()) > 5)
		{
			EmitSignal(SignalName.EndStep, side);
			footPos = pinPos + StepCalc(30);
		}
		
		for (var i = 1; i < points.Count; i++)
		{
			// distance constraint
			points[i] = points[i-1] + (points[i]-points[i-1]).Normalized() * distance;
			
			// constraint mid-joints to distance from footPos
			var dis = (footPos - points[i]).Length() - distance;
			if (i < points.Count-1 && Math.Abs(dis) < 5) continue;
			
			// determine rotation towards footPos
			var dir = footPos - points[i-1];
			var newPos = points[i-1] + dir.Normalized() * distance;
			var curAngle= (points[i] - points[i-1]).Angle();
			var desAngle= (newPos - points[i-1]).Angle();
			var dif = desAngle - curAngle;

			// adjust for Pi wrap
			float newAngle;
			if (dif > Math.PI) dif = (float)(desAngle - curAngle - 2 * Math.PI);
			if (dif < -Math.PI) dif = (float)(desAngle - curAngle + 2 * Math.PI);
			
				// mid.joint Rot towards/away from footPos to reach desired distance
			if (i < points.Count - 1 && Math.Abs(dis) > 5)
			{
				// if joint points perfectly at foot pos, add manual side rotation
				if ((pinPos-footPos).Normalized() - (points[i]-footPos).Normalized() < new Vector2(0.001f, 0.001f))
				{
					dif -= (float)side/10;
				}
				newAngle = curAngle + Math.Clamp(dif, -speed, speed) * (dis < 0 ? -1 : 1);
			}
			else newAngle = curAngle + Math.Clamp(dif, -speed, speed);

			var x = (float)(distance * Math.Cos(newAngle));
			var y = (float)(distance * Math.Sin(newAngle));
            
			points[i] = points[i-1] + new Vector2(x, y);
		}
	}
	
	public override void _Draw()
	{
		var a = new Vector2(10, 10);
		var b = new Vector2(-10, 10);
		
		
		DrawLine(footPos-a, footPos+a, Colors.DarkRed, 1, true);
		DrawLine(footPos+b, footPos-b, Colors.DarkRed, 1, true);
			
		for (var i = 1; i < points.Count; i++)
		{
			DrawCircle(points[0], distance*i ,new Color(.5f, .5f, .5f, .5f), false);
			DrawCircle(points[i], distance ,new Color(.5f, .5f, .5f, .3f), false);
			DrawLine(points[i-1], points[i], Colors.Black, 5, true);
		}
		for (var i = 1; i < points.Count; i++)
		{
			DrawCircle(points[i], 7, Colors.SteelBlue, true);
		}
		
		DrawCircle(pinPos, 10, Colors.Blue, true);
	}


}
