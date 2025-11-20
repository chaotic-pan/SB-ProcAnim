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
	[Export] public bool drawAsMesh = true;
	[Export] public Curve sizeCurve = new();
	[Export] public Color color = new(1, 1, 1);
	[Export] public Gradient gradient = new();

	private Leg[] legs = new Leg[1];
	public override void _Ready()
	{
		var pos = mousePos;
		for (int i = 0; i < count; i++)
		{
			points.Add(i, pos);
			pos.X += distance;
		}

		for (int i = 0; i < legs.Length; i++)
		{
			legs[i] = new Leg(points[((int)Math.Floor((decimal)(i/2))+1)*3], 
				i%2 == 0? new Vector2(0,1) : new Vector2(0,-1),3, 50, 0.1f);
			legs[i].Recalculate();
		}
	}

	public override void _Process(double delta)
	{
		if (Input.IsMouseButtonPressed(MouseButton.Left))
		{
			mousePos = GetGlobalMousePosition();
			Recalculate();
			QueueRedraw();
		}
	}

	private void Recalculate()
	{
		for (var i = 0; i < points.Count; i++)
		{
			var prev = i == 0 ? mousePos : points[i - 1];

			var dir = prev - points[i];
			var newPos = i == 0
				? points[i] + dir.Normalized() * float.Min(dir.Length(), speed)
				: prev + -dir.Normalized() * distance;

			float x, y;
			if (i < count-1)
			{
				var d = (prev - newPos).Dot(newPos - points[i + 1]);

				if (d < 750)
				{
					var av = prev - points[i + 1];
					newPos = i==0 ? points[0] + av.Normalized() * float.Min(dir.Length(), speed)
						: prev + -av.Normalized() * distance;
				}
			}

			points[i] = newPos;
		}
	}

	public override void _Draw()
	{
		for (int i = 0; i < legs.Length; i++)
		{
			legs[i].pinPos = points[((int)Math.Floor((decimal)(i/2))+1)*3];
			legs[i].Recalculate();
		}
		

		if (drawAsMesh) DrawPolygon();
		else DrawCircles();
		DrawLegs();
	}

	private void DrawLegs()
	{
		foreach (var leg in legs)
		{
			for (var i = 1; i < leg.points.Count; i++)
			{
				DrawLine(leg.points[i-1], leg.points[i], Colors.Black, 5, true);
			}
			for (var i = 1; i < leg.points.Count; i++)
			{
				DrawCircle(leg.points[i], 7, Colors.SteelBlue, true);
			}
		
			DrawCircle(leg.points[0], 10, Colors.Blue, true);
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
