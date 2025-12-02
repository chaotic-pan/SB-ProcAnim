using Godot;
using System;

public partial class SoftBody : Node2D
{
	[Export] private bool DEBUG_freeze;
	[Export] private Vector2 P1;
	[Export] private Vector2 P2;
	[Export] private Vector2 inertia;
	private Vector2 G = new(0, 9.81f);
	float m = 10;	// mass (in kg)
	
	Vector2[] p;	// position
	Vector2[] v;	// velocity
	private float radius = 100;
	private float dis = 100;
	[Export] private int count = 8;
	[Export] public Gradient gradient = new();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		p = new Vector2[count];
		v = new Vector2[count];
		
		for (int i = 0; i < count; i++)
		{
			var M = new Vector2(626, 324);
			
			var angle= Math.PI - (2*Math.PI/count)*(i+1);
			
			var x = (float)((radius) * Math.Cos(angle));
			var y = (float)((radius) * Math.Sin(angle));
			p[i] = M + new Vector2(x,y);
			v[i]  = p[i];
		}

		dis = (p[1] - p[0]).Length();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (DEBUG_freeze)
		{
			QueueRedraw();
			return;
		}
		for (int i = 0; i < count; i++)
		{
			var newP = 2*p[i] - v[i] + G*(float)delta;
			v[i] =  p[i]; 
			
			if (newP.Y >= P2.Y || newP.Y <= P1.Y )
			{
				var y = newP.Y - p[i].Y;
				newP.Y = p[i].Y - y*0.9f;
			}
			if (newP.X >= P2.X || newP.X <= P1.X)
			{
				var x = newP.X - p[i].X;
				newP.X = p[i].X - x*0.9f;
			}
			
			var damp = (newP - p[i])*0.99f;

			var prev = i==0? p[count-1] : p[i-1];
			var next = i==count-1? p[0] : p[i+1];
			
			var line1 = next - p[i];
			var line2 = prev - p[i];
			var m1 = (p[i] + line1 / 2);
			var m2 = (p[i] + line2 / 2);
			var c1 = (m1 - line1.Normalized()*dis/2);
			var c2 = (m2 - line2.Normalized()*dis/2);
			var move = (c1-p[i]+c2-p[i]).Normalized()*0.1f;
			p[i] += (damp + move/ 2) ;
		}
		
		
		QueueRedraw();
	}

	public override void _Draw()
	{
		for (int i = 0; i < count; i++)
		{
			DrawCircle(p[i], 10, gradient.Sample((float)i / count));
			// DrawCircle(p[i], 10, Colors.Black);
			DrawLine(i==0? p[count-1] : p[i-1], p[i], Colors.Black, 5);
		}
		
		
		DrawLine(P1, new (P1.X, P2.Y), Colors.White);
		DrawLine(new (P1.X, P2.Y), P2, Colors.White);
		DrawLine(P2, new (P2.X, P1.Y), Colors.White);
		DrawLine(new (P2.X, P1.Y), P1, Colors.White);
	}
}
