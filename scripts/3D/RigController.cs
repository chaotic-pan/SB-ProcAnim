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

	private Dictionary<int, Vector3[]> laggingBones = []; //boneIdx - pos, prevPos
	private Dictionary<int, Vector3[]> predictiveBones = []; //boneIdx - pos, prevPos
	private Bug3D[] bugs = new Bug3D[2];
	
	public override void _Ready()
	{
		skelli = GetChild<Skeleton3D>(0);
		for (int i = 0; i < skelli.GetBoneCount(); i++)
		{
			var rot = skelli.GetBoneGlobalPose(i).Basis.GetRotationQuaternion().GetEuler();
			if (skelli.GetBoneName(i).Contains("Tail") && !skelli.GetBoneName(i).Contains("_end")) 
				laggingBones.Add(i, [rot, rot]);
			if (skelli.GetBoneName(i).Contains("Head") && !skelli.GetBoneName(i).Contains("_end")) 
				predictiveBones.Add(i, [rot, rot]);
			
		}
		
		var iks = FindChildren("", "TwoBoneIK3D");
		for (int i = 0; i < iks.Count; i++)
		{
			bugs[i] = new Bug3D();
			bugs[i].Init(skelli, (TwoBoneIK3D)iks[i],this);
		}
	}

	public override void _Process(double delta)
	{
		#region Player Input Controls 
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
		#endregion

		#region tail lags
		foreach (int boneIdx in laggingBones.Keys)
		{
			Transform3D transform = skelli.GetBoneGlobalPose(boneIdx);
			var oldRot = laggingBones[boneIdx][1];
			var curRot = laggingBones[boneIdx][0];
			
			var parentIdx = skelli.GetBoneParent(boneIdx);
			var goalTrans = skelli.GetBoneGlobalPose(parentIdx);
			if(boneIdx == laggingBones.Keys.First()) goalTrans = goalTrans.RotatedLocal(Vector3.Forward, (float)Math.PI);
			var goalRot = goalTrans.Basis.GetRotationQuaternion().GetEuler();

			var angle = goalRot - curRot;
			var a = angle.X > Math.PI ? 1 : angle.X < -Math.PI ? -1 : 0;
			var b = angle.Y > Math.PI ? 1 : angle.Y < -Math.PI ? -1 : 0;
			var c = angle.Z > Math.PI ? 1 : angle.Z < -Math.PI ? -1 : 0;
		
			oldRot += new Vector3(a*Single.Tau, b*Single.Tau, c*Single.Tau);
			curRot += new Vector3(a*Single.Tau, b*Single.Tau, c*Single.Tau);

			var newRot = (2*curRot - oldRot + (goalRot-curRot)*(turn/1000f));
			var dif = newRot - curRot;
		
			dif *= 1 - (damp / 100f);
			newRot = curRot + dif;
			
			transform.Basis = new Basis(Quaternion.FromEuler(newRot));
			skelli.SetBoneGlobalPose(boneIdx, transform);
			laggingBones[boneIdx] = [newRot, curRot];
		}
		#endregion 
		
		#region head turn
		foreach (int boneIdx in predictiveBones.Keys)
		{
			Transform3D transform = skelli.GetBoneGlobalPose(boneIdx);
			var oldRot = predictiveBones[boneIdx][1];
			var curRot = predictiveBones[boneIdx][0];
			
			var parentIdx = skelli.GetBoneParent(boneIdx);
			var goalTrans = skelli.GetBoneGlobalPose(parentIdx);
			var goalRot = goalTrans.Basis.GetRotationQuaternion().GetEuler();

			var angle = goalRot - curRot;
			var a = angle.X > Math.PI ? 1 : angle.X < -Math.PI ? -1 : 0;
			var b = angle.Y > Math.PI ? 1 : angle.Y < -Math.PI ? -1 : 0;
			var c = angle.Z > Math.PI ? 1 : angle.Z < -Math.PI ? -1 : 0;
		
			oldRot += new Vector3(a*Single.Tau, b*Single.Tau, c*Single.Tau);
			curRot += new Vector3(a*Single.Tau, b*Single.Tau, c*Single.Tau);
			
			// prev parent
			// dif to current parent
			// pos = current parent + dif
			var newRot = 2*curRot - oldRot + (goalRot-curRot)*(turn/1000f);
			var dif = newRot - curRot;
		
			dif *= 1 - (damp / 100f);
			newRot = curRot - dif;
			
			transform.Basis = new Basis(Quaternion.FromEuler(newRot));
			skelli.SetBoneGlobalPose(boneIdx, transform);
			predictiveBones[boneIdx] = [newRot, curRot];
		}
		#endregion 
		
		bugs[0].Update(delta);
		
	}
}
