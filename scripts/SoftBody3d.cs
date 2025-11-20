using Godot;
using System;

public partial class SoftBody3d : SoftBody3D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		GD.Print();
	}

	private void Move(Vector3 pos)
	{
		GD.Print("moving o7");
		Position = pos;
	}
}
