using Godot;
using System;

public partial class Leg_alt : Node2D
{
	[Export] public Vector2 PinPos; //root
	[Export] public float Dis1; //lenght arm1
	[Export] public float Dis2; //lenght arm2
	[Export] public float Speed;
	[Export] public bool DrawRanges = true;

	private Vector2 target; //target position of hand
	private Vector2 pole; //direction to bend towards 

	private Vector2 elbowPos;
	private Vector2[] M = [];
	
	private Vector2[] points = new Vector2 [3];
	private Vector2 step;
	private int side;
	private Vector2 orientation;
	
	private bool targetGrabbed;
	private bool poleGrabbed;
	private bool solo = true;
	

	public Leg_alt()
	{
	}

	public Leg_alt(Vector2 pinPos, Vector2 orientation, int side, float dis1, float dis2, float speed, bool drawRanges)
	{
		PinPos = pinPos;
		this.orientation = orientation;
		this.side = side;
		Dis1 = dis1;
		Dis2 = dis2;
		this.Speed = speed;
		this.DrawRanges = drawRanges;
		solo = false;
		
		pole = pinPos-orientation*150;
		
		var angle= orientation.Angle() + side*(Math.PI/2);
		// adjust for Pi wrap
		if (angle > Math.PI) angle -= 2 * Math.PI;
		if (angle < -Math.PI) angle += 2 * Math.PI;
		var direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
		target = pinPos+direction*50;
		
		QueueRedraw();
	}
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (solo)
		{
			target = new Vector2(800, 100);
			pole = new Vector2(target.X, PinPos.Y);
		}
	}
	
	public override void _Process(double delta)
	{
		if (solo)
		{
			var mousePos = GetGlobalMousePosition();
			if (Input.IsMouseButtonPressed(MouseButton.Left))
			{
				if ((mousePos - pole).Length() <= 10 && !targetGrabbed)
				{
					poleGrabbed = true;
				}
				else if ((mousePos - target).Length() <= 10)
				{
					targetGrabbed = true;
				}
			}
			else
			{
				targetGrabbed = false;
				poleGrabbed = false;
			}

			if (poleGrabbed)
			{
				pole = mousePos;
			}
			else if (targetGrabbed)
			{
				var mouseDis = Math.Abs((mousePos - PinPos).Length());
				if (mousePos == PinPos)
				{
					mousePos += Vector2.One;
				}

				if (mouseDis >= Math.Abs(Dis1 + Dis2))
				{
					target = PinPos + (mousePos - PinPos).Normalized() * Math.Abs(Dis1 + Dis2 - 1);
				}
				else if (mouseDis <= Math.Abs(Dis1 - Dis2))
				{
					target = PinPos + (mousePos - PinPos).Normalized() *
						Math.Abs(Dis1 - Dis2 + (Dis1 >= Dis2 ? 1 : -1));
				}
				else
				{
					target = mousePos;
				}

			}
		}
		else
		{
			// get new footPos when outta range 
			if ((target-PinPos).Length() >= Dis1+Dis2-1)
			{
				GD.Print("AAAAAAAAAAAAAAAAAAA");
				target = PinPos + StepCalc(30);
				// EmitSignal(SignalName.StartStep, side);
				// step = true;
			}
			// + keep updating til foot reached it 
		// 	if (step && Math.Abs((points[count - 1]-Target).Length()) > 5)
		// 	{
		// 		// EmitSignal(SignalName.EndStep, side);
		// 		Target = PinPos + StepCalc(30);
		// 	}
		}
		
		M = CircleIntersections(PinPos, Dis1, target, Dis2);
		if (M.Length == 1) elbowPos = M[0];
		else
		{
			var d1 = Math.Abs((M[0] - pole).Length());
			var d2 = Math.Abs((M[1] - pole).Length());
			elbowPos = d1 <= d2 ? M[0] : M[1];
		}

		QueueRedraw();
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
		
		var angle = (PinPos - target).Angle();
		var c = M + new Vector2((float)Math.Cos(angle+Math.PI/2), (float)Math.Sin(angle+Math.PI/2)).Normalized() * (float)h; 
		var d = M + new Vector2((float)Math.Cos(angle-Math.PI/2), (float)Math.Sin(angle-Math.PI/2)).Normalized() * (float)h;

		return [c, d];
	}

	public override void _Draw()
	{
		var a = new Vector2(10, 10);
		var b = new Vector2(-10, 10);
		if (DrawRanges)
		{
			DrawCircle(PinPos, Dis1+Dis2, new Color(0.7f,0.8f,1,0.2f), true);
			DrawCircle(PinPos, Dis1-Dis2, new Color(0.3f,0.3f,0.3f), true);
			DrawCircle(elbowPos, Dis2, new Color(.2f,.5f,1), false, 2);
		
			DrawCircle(PinPos, Dis1, new Color(.3f,.3f,.3f), false, 2);
			DrawCircle(target, Dis2, new Color(.3f,.3f,.3f), false, 2);
		
			DrawDashedLine(PinPos, target, Colors.DeepPink, 2, 5);
			if (M.Length == 1)
			{
				DrawCircle(M[0], 5, Colors.DeepPink, true);
			}
			else if (M.Length == 2)
			{
				DrawDashedLine(M[0], M[1], Colors.DeepPink, 2, 5); 
			}
			
			var step = PinPos + StepCalc(30);
			DrawLine(step - a / 2, step + a / 2, Colors.Blue, 1, true);
			DrawLine(step + b / 2, step - b / 2, Colors.Blue, 1, true);
		}
		
        
		// Arms
		DrawLine(PinPos, elbowPos, Colors.Black, 5, true);
		DrawLine(elbowPos, target, Colors.Black, 5, true);
		// Joints
		DrawCircle(PinPos, 10, Colors.Blue, true);
		DrawCircle(elbowPos, 7, Colors.SteelBlue, true);
		DrawCircle(target, 7, Colors.SteelBlue, true);
		
		// Pole
		DrawCircle(pole, 10, Colors.DimGray, true);
		DrawCircle(pole, 10, Colors.DarkRed, false, 2);
	
		// Target
		DrawLine(target-a, target+a, Colors.DarkRed, 1, true);
		DrawLine(target+b, target-b, Colors.DarkRed, 1, true);
	}

	public void triggerStep()
	{
		target = PinPos + StepCalc(30);
	}

	public void triggerRest()
	{
		throw new NotImplementedException();
	}

	public void Refresh(Vector2 pos, Vector2 orientation)
	{
		PinPos = pos;
		pole = pos-orientation*150;
		this.orientation = orientation;
	}
	
	private Vector2 StepCalc(float offsetAngle)
	{
		// set point +/- 30° from orientation
		var angle= orientation.Angle() + side*(Math.PI / 180 * offsetAngle);
			
		// adjust for Pi wrap
		if (angle > Math.PI) angle -= 2 * Math.PI;
		if (angle < -Math.PI) angle += 2 * Math.PI;
			
		var x = (float)((Dis1+Dis2-1) * Math.Cos(angle));
		var y = (float)((Dis1+Dis2-1) * Math.Sin(angle));
		return new Vector2(x,y);
	}
}
