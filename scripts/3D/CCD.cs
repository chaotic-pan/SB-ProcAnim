using Godot;
using System;

public partial class CCD : Node3D
{
	private Node3D hip;
	private Node3D knee;
	private Node3D foot;
	private Node3D pole;
	public CCD(Node3D hip, Node3D knee, Node3D foot, Node3D pole)
	{
		this.hip = hip;
		this.knee = knee;
		this.foot = foot;
		this.pole = pole;
	}

	public void Recalculate(Node3D targetUp, Node3D targetLow)
	{
		hip.GlobalPosition = targetUp.GlobalPosition;
		hip.LookAt(pole.GlobalPosition);
		knee.GlobalRotation = hip.GlobalRotation;

		var dis = foot.GlobalPosition.DistanceTo(targetLow.GlobalPosition);
		while (dis > 0.01)
		{
			dis = Iterate(targetLow);
		}
	}

	private float Iterate(Node3D targetLow)
	{
		knee.LookAt(targetLow.GlobalPosition);
		foot.LookAt(foot.GlobalPosition + Vector3.Left);
        		
		var vA = foot.GlobalPosition - hip.GlobalPosition;
		var vB = targetLow.GlobalPosition - hip.GlobalPosition;
		var angle = vB.AngleTo(vA);
		hip.Rotate((vA.Cross(vB)).Normalized(), angle);

		return foot.GlobalPosition.DistanceTo(targetLow.GlobalPosition);
	}
}
