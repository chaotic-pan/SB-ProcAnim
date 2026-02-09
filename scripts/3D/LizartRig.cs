using Godot;
using System;

public partial class LizartRig : Node3D
{
	[Export] private float turnSpeed= 1f;
	[Export] private float moveSpeed= 1f;
	private Skeleton3D skelli;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		skelli = GetChild<Skeleton3D>(0);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		var pos = skelli.GetBonePosePosition(0);
		var rot = skelli.GetBonePoseRotation(0).Normalized();
		var eul = rot.GetEuler();
		
		if (Input.IsKeyPressed(Key.Left))
			eul.Z += turnSpeed/200;
		else if (Input.IsKeyPressed(Key.Right))
			eul.Z -= turnSpeed/200;
		
		var move = new Vector3(0,moveSpeed/10000,0).Rotated(eul.Normalized(), eul.Length());
		if (Input.IsKeyPressed(Key.Up))
			pos += move;
		else if (Input.IsKeyPressed(Key.Down))
			pos -= move;
		
		skelli.SetBonePosePosition(0, pos);
		skelli.SetBonePoseRotation(0, Quaternion.FromEuler(eul));
	}
}
