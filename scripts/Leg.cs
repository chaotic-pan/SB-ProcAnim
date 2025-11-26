using Godot;
using System;
using System.Collections.Generic;

public partial class Leg : Node2D
{
	private Vector2[] points = new Vector2 [3];
	private Vector2 pinPos = new(626, 324);
	private Vector2 footPos;
	private Vector2 orientation;
	private int side = 1;
	[Export] public float Dis1 = 20;
	[Export] public float Dis2 = 20;
	[Export] public float Speed = 5;
	[Export] public bool DrawRanges = true;
	private bool solo = true;
	[Signal] public delegate void StartStepEventHandler(int side);
	[Signal] public delegate void EndStepEventHandler(int side);
	
	public Leg()
	{
	}

	public Leg(Vector2 pinPos, Vector2 orientation, int side, float distanceUp, float distanceLow, float speed, bool drawRanges)
	{
		this.pinPos = pinPos;
		this.orientation = orientation;
		this.side = side;
		Dis1 = distanceUp;
		Dis2 = distanceLow;
		Speed = speed;
		DrawRanges = drawRanges;
		solo = false;
		
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
		if (solo) Init(new Vector2(1,0));
	}

	private void Init(Vector2 direction)
	{
		
		points[0] = pinPos;
		points[1] = pinPos+direction*Dis1;
		points[2] = pinPos+direction*(Dis1+Dis2);

		footPos = pinPos + direction*(Dis1+Dis2)*0.75f;
	}
	
	public override void _Process(double delta)
	{
		if (solo)
		{
			if (Input.IsKeyPressed(Key.S)) pinPos.Y++;
			if (Input.IsKeyPressed(Key.W)) pinPos.Y--;
			if (Input.IsKeyPressed(Key.D)) pinPos.X++;
			if (Input.IsKeyPressed(Key.A)) pinPos.X--;

			if (Input.IsMouseButtonPressed(MouseButton.Left))
			{
				footPos = GetGlobalMousePosition();
				step = false;
			}
			
			if (pinPos != points[0]) orientation = (pinPos - points[0]).Normalized();
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
		footPos = pinPos + StepCalc(90).Normalized() * ((Dis1+Dis2)*0.25f);
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
			
		var x = (float)((Dis1+Dis2) * Math.Cos(angle));
		var y = (float)((Dis1+Dis2) * Math.Sin(angle));
		return new Vector2(x,y);
	}

	private bool step = false;

	public void Recalculate()
	{
		points[0] = pinPos;

		// get new footPos when outta range 
		if ((footPos - pinPos).Length() > (Dis1 + Dis2))
		{
			footPos = pinPos + StepCalc(30);
			// EmitSignal(SignalName.StartStep, side);
			step = true;
		}

		// + keep updating til foot reached it 
		if (step && Math.Abs((points[2] - footPos).Length()) > 5)
		{
			// EmitSignal(SignalName.EndStep, side);
			footPos = pinPos + StepCalc(30);
		}
		
		var M = CircleIntersections(pinPos, Dis1, footPos, Dis2);
		if (M.Length == 1) points[1] = M[0];
		else
		{
			var pole = pinPos-orientation*(Dis1+Dis2);
			var d1 = Math.Abs((M[0] - pole).Length());
			var d2 = Math.Abs((M[1] - pole).Length());
			points[1] = d1 <= d2 ? M[0] : M[1];
		}
		points[2] = footPos;

	}
	
	private Vector2[] CircleIntersections(Vector2 a, float r1, Vector2 b, float r2)
	{
		// distance between circles
		var dAB = (b - a).Length();

		// normalized radiuses
		var r1n = r1 / dAB;
		var r2n = r2 / dAB;

		// ratio along unit line
		var rX = (Math.Pow(r1n, 2) - Math.Pow(r2n, 2) + 1) / 2;

		// C = middle point of intersection
		var M = new Vector2((float)(a.X + (b.X - a.X) * rX), (float)(a.Y + (b.Y - a.Y) * rX));
		
		var h = Math.Sqrt(Math.Pow(r1n,2) - Math.Pow(((Math.Pow(r1n,2) - Math.Pow(r2n,2) +1) /2),2)) * dAB;
		if (h == 0)
		{
			return [M];
		}
		
		var angle = (pinPos - footPos).Angle();
		var c = M + new Vector2((float)Math.Cos(angle+Math.PI/2), (float)Math.Sin(angle+Math.PI/2)).Normalized() * (float)h; 
		var d = M + new Vector2((float)Math.Cos(angle-Math.PI/2), (float)Math.Sin(angle-Math.PI/2)).Normalized() * (float)h;

		return [c, d];
	}

	public override void _Draw()
	{
		var a = new Vector2(10, 10);
		var b = new Vector2(-10, 10);

		var step = pinPos + StepCalc(30);

		if (DrawRanges)
		{
			// reachable area
			DrawCircle(points[0], Dis1+Dis2, new Color(0.7f,0.8f,1,0.2f), true);
			DrawCircle(points[0], Dis1-Dis2, new Color(0.3f,0.3f,0.3f), true);
			DrawCircle(points[1], Dis2, new Color(.2f,.5f,1), false, 2);
			
			// leg ranges
			DrawCircle(points[0], Dis1, new Color(.3f,.3f,.3f), false, 2);
			DrawCircle(points[2], Dis2, new Color(.3f,.3f,.3f), false, 2);
		
			// new step
			DrawLine(step - a / 2, step + a / 2, Colors.DimGray, 1, true);
			DrawLine(step + b / 2, step - b / 2, Colors.DimGray, 1, true);
			
			// current footPos
			DrawLine(footPos-a, footPos+a, Colors.DarkRed, 1, true);
			DrawLine(footPos+b, footPos-b, Colors.DarkRed, 1, true);
			
			var pole = pinPos-orientation*(Dis1+Dis2);
			DrawCircle(pole, 10, Colors.DimGray, true);
			DrawCircle(pole, 10, Colors.DarkRed, false, 2);
			
			DrawDashedLine(points[0], points[2], Colors.DeepPink, 2, 5);
			// if (M.Length == 1)
			// {
			// 	DrawCircle(M[0], 5, Colors.DeepPink, true);
			// }
			// else if (M.Length == 2)
			// {
			// 	DrawDashedLine(M[0], M[1], Colors.DeepPink, 2, 5); 
			// }
			
			// orientation
			DrawLine(pinPos, pinPos+orientation*30, Colors.Blue, 1, true);
		}
		
		
		DrawLine(points[0], points[1], Colors.Black, 5, true);
		DrawLine(points[1], points[2], Colors.Black, 5, true);
		
		DrawCircle(points[1], 7, Colors.SteelBlue, true);
		DrawCircle(points[2], 7, Colors.SteelBlue, true);
		
		DrawCircle(pinPos, 10, Colors.Blue, true);
	}


	public void Refresh(Vector2 pos, Vector2 orientation)
	{
		pinPos = pos;
		this.orientation = orientation;
		Recalculate();
	}
}
