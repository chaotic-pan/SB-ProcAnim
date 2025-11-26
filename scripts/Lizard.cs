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
	[Export] public bool drawRanges = true;
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
			bugs[j] = new Bug(points[bugPos-2], points[bugPos], legCounts[bugPos], 
				jointDistance, legSpeed, speed, drawRanges);
			AddChild(bugs[j]);
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
			bugs[j].pos = points[bugPos];
			bugs[j].lookPos = points[bugPos-2];
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
