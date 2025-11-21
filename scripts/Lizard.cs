using Godot;
using System;
using Godot.Collections;

public partial class Lizard : Node2D 
{
	Dictionary<int, Vector2> points = new();
	private Vector2 mousePos = new(626, 324);
	[Export] public int count = 5;
	[Export] public float distance = 20;
	[Export] public float speed = 5;
	[Export] public Dictionary<int, int> legCounts = new();
	[Export] public int jointCount;
	[Export] public int jointDistance;
	[Export] public float legSpeed;
	[Export] public bool drawAsMesh = true;
	[Export] public Curve sizeCurve = new();
	[Export] public Color color = new(1, 1, 1);
	[Export] public Gradient gradient = new();

	private Bug[] bugs;
	public override void _Ready()
	{
		var pos = mousePos;
		for (int i = 0; i < count; i++)
		{
			points.Add(i, pos);
			pos.X += distance;
		}

		bugs = new Bug[legCounts.Count];
		var j = 0;
		foreach (var bugPos in legCounts.Keys)
		{
			bugs[j] = new Bug(points[bugPos], (points[bugPos-1]-points[bugPos]).Normalized(), legCounts[bugPos], 
				jointCount, jointDistance, legSpeed, speed);
			// AddChild(bugs[j]);
			j++;
		}
	}

	public override void _Process(double delta)
	{
		if (Input.IsMouseButtonPressed(MouseButton.Left)) mousePos = GetGlobalMousePosition();
		
		Recalculate();
		var j = 0;
		foreach (var bugPos in legCounts.Keys)
		{
			bugs[j].pinPos = points[bugPos];
			bugs[j].orientation = (points[0] - points[bugPos]).Normalized();;
			bugs[j].Recalculate();
			j++;
		}
		QueueRedraw();
		
	}

	private void Recalculate()
	{
		for (var i = 0; i < points.Count; i++)
		{
			// move towards mouse OR follow prev exactly
			var prev = i == 0 ? mousePos : points[i - 1];

			var dir = prev - points[i];
			var newPos = i == 0
				? points[i] + dir.Normalized() * float.Min(dir.Length(), speed)
				: prev + -dir.Normalized() * distance;

			float x, y;
			if (i < count-1)
			{
				var d = (prev - newPos).Normalized().Dot((newPos - points[i + 1]).Normalized());

				if (Math.Abs(d) > 0.7)
				{
					// var av = prev - points[i + 1];
					// newPos = i==0 ? points[0] + av.Normalized() * float.Min(dir.Length(), speed)
					// 	: prev + -av.Normalized() * distance;
				}
			}

			points[i] = newPos;
		}
	}

	public override void _Draw()
	{
		if (drawAsMesh) DrawPolygon();
		else DrawCircles();
		DrawLegs();
	}

	private void DrawLegs()
	{
		foreach (var bug in bugs)
		{
			// orientation
			DrawLine(bug.pos, bug.pinPos+bug.orientation*30, Colors.DimGray, 1, true);
			
			var a = new Vector2(10, 10);
			var b = new Vector2(-10, 10);
			
			foreach (var leg in bug.legs)
			{
				// foot X
				DrawLine(leg.footPos-a, leg.footPos+a, Colors.DarkRed, 1, true);
				DrawLine(leg.footPos+b, leg.footPos-b, Colors.DarkRed, 1, true);
			
				// leg
				for (var i = 1; i < leg.points.Count; i++)
				{
					DrawLine(leg.points[i-1], leg.points[i], Colors.Black, 3, true);
				}
				// joint
				for (var i = 1; i < leg.points.Count; i++)
				{
					DrawCircle(leg.points[i], 7, Colors.SteelBlue, true);
				}
			}
		
			// pin
			DrawCircle(bug.pos, 10, Colors.DimGray, true);
			DrawCircle(bug.pos, jointDistance * jointCount, new Color(.5f, .5f, .5f, .3f), false);
		}
	}

	private void DrawCircles()
	{
		for (var i = 0; i < points.Count; i++)
		{
			var size = sizeCurve.Sample((float)i / count);
			DrawCircle(points[i], size, gradient.Sample((float)i / count));
		}
	}

	private void DrawPolygon()
	{
		Vector2[] verx = new Vector2[count*2];
		float x, y;
		
		for (int i = 0; i < points.Count; i++)
		{
			var size = sizeCurve.Sample((float)i / count);
			
			if (i == 0)
			{
				x = (float)(points[i].X + size * Math.Cos((points[i] - points[i + 1]).Angle()));
				y = (float)(points[i].Y + size * Math.Sin((points[i] - points[i + 1]).Angle()));
				verx[0] = new Vector2(x, y);
			}
			
			if (i == count - 1)
			{
				verx[count] = points[i];
				continue;
			}
			
			x = (float)(points[i].X + size * Math.Cos((points[i] - points[i + 1]).Angle() + Math.PI / 2));
			y = (float)(points[i].Y + size * Math.Sin((points[i] - points[i + 1]).Angle() + Math.PI / 2));
			verx[i + 1] = new Vector2(x, y);
			x = (float)(points[i].X + size * Math.Cos((points[i] - points[i + 1]).Angle() - Math.PI / 2));
			y = (float)(points[i].Y + size * Math.Sin((points[i] - points[i + 1]).Angle() - Math.PI / 2));
			verx[count * 2 - i - 1] = new Vector2(x, y);
		}
		
		DrawColoredPolygon(verx, color);
	}
}
