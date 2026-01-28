using Godot;
using System;

public partial class LizartRig : Node3D
{
	private Skeleton3D skelli;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		skelli = GetChild(0).GetChild<Skeleton3D>(0);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		var pos = skelli.GetBonePosePosition(0);
		var rot = skelli.GetBonePoseRotation(0).Normalized();
		var eul = rot.GetEuler();

		
		if (Input.IsKeyPressed(Key.Left))
			eul.Y -= 0.01f;
		else if (Input.IsKeyPressed(Key.Right))
			eul.Y += 0.01f;
		if (Input.IsKeyPressed(Key.Up))
			pos -= new Vector3(0,0, 0.01f).Rotated(Vector3.Up, eul.Y);
		else if (Input.IsKeyPressed(Key.Down))
			pos += new Vector3(0,0, 0.01f).Rotated(Vector3.Up, eul.Y);
		
		skelli.SetBonePosePosition(0, pos);
		skelli.SetBonePoseRotation(0, Quaternion.FromEuler(eul));
	}
}
