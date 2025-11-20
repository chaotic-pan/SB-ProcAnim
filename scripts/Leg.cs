using Godot;
using System;
using System.Collections.Generic;

public partial class Leg : Node2D
{
	public Dictionary<int, Vector2> points = new();
	public Vector2 pinPos = new(626, 324);
	public Vector2 footPos;
	public Vector2 stepPos;
	[Export] public int count = 5;
	[Export] public float distance = 20;
	[Export] public float speed = 5;
	private bool solo = false;

	public Leg(Vector2 pinPos, Vector2 direction, int count, float distance, float speed)
	{
		this.pinPos = pinPos;
		this.count = count;
		this.distance = distance;
		this.speed = speed;
		var pos = pinPos;
		
		for (int i = 0; i < count; i++)
		{
			points.Add(i, pos);
			pos += direction*distance;
		}

		footPos = points[count - 2] + direction*(distance*0.5f);
		Recalculate();
	}

	public Leg()
	{
	}

	public override void _Ready()
	{
		count++;
		solo = true;
		var pos = pinPos;
		for (int i = 0; i < count; i++)
		{
			points.Add(i, pos);
			pos.X += distance;
		}

		footPos = pinPos + new Vector2(1,0)*(distance*(count-1))*0.75f;
	}
	
	public override void _Process(double delta)
	{
		if (!solo) return;
		if (Input.IsKeyPressed(Key.S)) pinPos.Y++;
		if (Input.IsKeyPressed(Key.W)) pinPos.Y--;
		if (Input.IsKeyPressed(Key.D)) pinPos.X++;
		if (Input.IsKeyPressed(Key.A)) pinPos.X--;

		if (Input.IsMouseButtonPressed(MouseButton.Left)) footPos = GetGlobalMousePosition();
		
		Recalculate();
		QueueRedraw();

	}

	public void triggerStep(Vector2 step)
	{
		stepPos = step;
		footPos = step;
		Recalculate();
	}

	public void Recalculate()
	{
		// body has moved
		var direction = (pinPos-points[0]).Normalized();
		points[0] = pinPos;
		
		// set new footPos is range
		if ((footPos-pinPos).Length() > distance*(count-1))
		{
			footPos = stepPos;
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
				if ((pinPos-footPos).Normalized() == (points[i]-footPos).Normalized())
				{
					dif += (float)Math.PI;
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
