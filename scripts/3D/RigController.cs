using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class RigController : Node3D
{
	[Export] private float turnSpeed= 1f;
	[Export] private float moveSpeed= 1f;
	private Skeleton3D skelli;
	[Export] private float turn;
	[Export] private float damp;

	private Dictionary<int, Vector3[]> laggingBones = [];
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		skelli = GetChild<Skeleton3D>(0);
		for (int i = 0; i < skelli.GetBoneCount(); i++)
		{
			var rot = skelli.GetBoneGlobalPose(i).Basis.GetRotationQuaternion().GetEuler();
			if (skelli.GetBoneName(i).Contains("Tail") && !skelli.GetBoneName(i).Contains("_end")) 
				laggingBones.Add(i, [rot, rot]);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// player input controlls
		var pos = skelli.GetBonePosePosition(0);
		var rot = skelli.GetBonePoseRotation(0).Normalized();
		var eul = rot.GetEuler();

		if (Input.IsKeyPressed(Key.Left))
			eul.Z += turnSpeed / 200;
		else if (Input.IsKeyPressed(Key.Right))
			eul.Z -= turnSpeed / 200;

		var move = new Vector3(0, moveSpeed / 10000, 0).Rotated(eul.Normalized(), eul.Length());
		if (Input.IsKeyPressed(Key.Up))
			pos += move;
		else if (Input.IsKeyPressed(Key.Down))
			pos -= move;

		skelli.SetBonePosePosition(0, pos);
		skelli.SetBonePoseRotation(0, Quaternion.FromEuler(eul));

		// tail lags
		foreach (int boneIdx in laggingBones.Keys)
		{
			Transform3D transform = skelli.GetBoneGlobalPose(boneIdx);
			var oldRot = laggingBones[boneIdx][1];
			var curRot = laggingBones[boneIdx][0];
			
			var parentIdx = skelli.GetBoneParent(boneIdx);
			var goalTrans = skelli.GetBoneGlobalPose(parentIdx);
			if(boneIdx == laggingBones.Keys.First()) goalTrans = goalTrans.RotatedLocal(Vector3.Forward, (float)Math.PI);
			var goalRot = goalTrans.Basis.GetRotationQuaternion().GetEuler();
			
			// VERLET INTEGRATION	 newPos = 2*pos - prevPos + acceleration
			var newRot = (2*curRot - oldRot + (goalRot-curRot)*(turn/1000f));

			// DAMP
			newRot = (curRot + (newRot - curRot)*(1-(damp/100f)));

			//
			// var m = (newRot - curRot).Length();		
			// if (m > 1)
			// {
			// 	GD.Print($"{newRot} // {curRot}");
			// }
			transform.Basis = new Basis(Quaternion.FromEuler(newRot));
			skelli.SetBoneGlobalPose(boneIdx, transform);
			laggingBones[boneIdx] = [newRot, curRot];
		}
	}
}
