using Godot;
using System;
using System.Collections;

public partial class Leg3D : Node3D
{
	[Export] private bool drawDebugs;
	[Export] private Material yellow;
	[Export] private Material blue;
	[Export] private Material red;
	[Export] private float speed = 0.01f;
	private Node3D hip;
	private Node3D knee;
	private Node3D foot;
	private Node3D pole;
	// private Node3D gizmo;
	private Node3D targetUp;
	private Node3D targetLow;
	private const float PI = (float)Math.PI;
	private const float DIS = 3f;
	
	private Node3D debugTorus;
	private Node3D debugCylinderLine;
	private Node3D debugCylinderU;
	private Node3D debugCylinderP;
	private Node3D debugCylinderProj;
	public override void _Ready()
	{
		hip = GetNode<Node3D>("%Hip");
		knee = GetNode<Node3D>("%Knee");
		foot = GetNode<Node3D>("%Foot");
		pole = GetNode<Node3D>("%Pole");
		// gizmo = GetNode<Node3D>("%Gizmo");
		targetUp = GetNode<Node3D>("%TargetUp");
		targetLow = GetNode<Node3D>("%TargetLow");

		if (drawDebugs)
		{
			// intersection circle
			var torus = new TorusMesh();
			torus.SurfaceSetMaterial(0, red);
			var n = new Node3D();
			var node = new MeshInstance3D();
			node.Mesh = torus;
			var line = targetUp.GlobalPosition - targetLow.GlobalPosition;
			var D = DIS / line.Length();
			torus.OuterRadius = (float)(Math.Sqrt(D * D - (1f / 4f)) * line.Length()); // assumes equal bone lengths
			AddChild(n);
			n.AddChild(node);
			node.Rotation = new Vector3(PI / 2, 0, 0);
			debugTorus = n;

			// line targetLow-targetUp
			var cylinder = new CylinderMesh();
			cylinder.SurfaceSetMaterial(0, red);
			n = new Node3D();
			node = new MeshInstance3D();
			node.Mesh = cylinder;
			cylinder.BottomRadius = 0.02f;
			cylinder.TopRadius = 0.02f;
			AddChild(n);
			n.AddChild(node);
			node.Rotation = new Vector3(PI / 2, 0, 0);
			node.Position = new Vector3(0, 0, -cylinder.Height / 2);
			debugCylinderLine = n;
			
			// intersection plane vector U
			cylinder = new CylinderMesh();
			cylinder.SurfaceSetMaterial(0, red);
			n = new Node3D();
			node = new MeshInstance3D();
			node.Mesh = cylinder;
			cylinder.BottomRadius = 0.02f;
			cylinder.TopRadius = 0.02f;
			AddChild(n);
			n.AddChild(node);
			node.Rotation = new Vector3(PI / 2, 0, 0);
			node.Position = new Vector3(0, 0, -cylinder.Height / 2);
			debugCylinderU = n;

			// mid-pole vector P
			cylinder = new CylinderMesh();
			cylinder.SurfaceSetMaterial(0, blue);
			n = new Node3D();
			node = new MeshInstance3D();
			node.Mesh = cylinder;
			cylinder.BottomRadius = 0.02f;
			cylinder.TopRadius = 0.02f;
			cylinder.Height = 10;
			AddChild(n);
			n.AddChild(node);
			node.Rotation = new Vector3(PI / 2, 0, 0);
			node.Position = new Vector3(0, 0, -cylinder.Height / 2);
			debugCylinderP = n;
			
			// projection of P onto intersection plane
			cylinder = new CylinderMesh();
			cylinder.SurfaceSetMaterial(0, yellow);
			n = new Node3D();
			node = new MeshInstance3D();
			node.Mesh = cylinder;
			cylinder.BottomRadius = 0.02f;
			cylinder.TopRadius = 0.02f;
			AddChild(n);
			n.AddChild(node);
			node.Rotation = new Vector3(PI / 2, 0, 0);
			node.Position = new Vector3(0, 0, -cylinder.Height / 2);
			debugCylinderProj = n;
		}
		else
		{
			targetUp.GetChild<MeshInstance3D>(2).Transparency = 1;
			targetLow.GetChild<MeshInstance3D>(2).Transparency = 1;
		}

		Recalculate();
	}
	
	public override void _Process(double delta)
	{
		targetUp.GetChild<MeshInstance3D>(1).MaterialOverride = blue;
		targetLow.GetChild<MeshInstance3D>(1).MaterialOverride = blue;
		pole.GetChild<MeshInstance3D>(1).MaterialOverride = blue;
		
		if (isGrabbed != null)
		{
			isGrabbed.GetChild<MeshInstance3D>(1).MaterialOverride = yellow;
			var pos = isGrabbed.GlobalPosition;
			if (Input.IsKeyPressed(Key.W))
				pos.Y += 0.01f;
			if (Input.IsKeyPressed(Key.S))
				pos.Y -= 0.01f;
			if (Input.IsKeyPressed(Key.A))
				pos.Z -= 0.01f;
			if (Input.IsKeyPressed(Key.D))
				pos.Z += 0.01f;
			if (Input.IsKeyPressed(Key.Q))
				pos.X -= 0.01f;
			if (Input.IsKeyPressed(Key.E))
				pos.X += 0.01f;
			if (isGrabbed == pole ||
				(pos.DistanceTo(targetLow.GlobalPosition) < DIS * 2 &&
			    pos.DistanceTo(targetUp.GlobalPosition) < DIS * 2))
			{
				isGrabbed.GlobalPosition = pos;
				Recalculate();
			}
		}

	}

	private void Recalculate()
	{
		hip.GlobalPosition = targetUp.GlobalPosition;
		
		var line = targetUp.GlobalPosition - targetLow.GlobalPosition;
		var mid = targetLow.GlobalPosition + line/2;
		
		var d =  DIS / line.Length();
		var R = (float)Math.Sqrt(d * d - (1f / 4f)) * line.Length(); // assumes equal bone lengths
		
		// perpendicular to line
		Vector3 U = line.X != 0? new Vector3(-line.Y / line.X, 1, 0) :
			line.Y != 0? new Vector3(0, -line.Z / line.Y, 1) : new Vector3(1, 0, -line.X / line.Z);
		
		var P = pole.GlobalPosition - mid;
		var N = (float)((P.Dot(line))/Math.Pow(line.Length(),2)) *line; // projection of K onto line
		var proj = P - N; // projection of K onto intersection plane
		
		hip.LookAt(mid+proj.Normalized()*R);
		knee.LookAt(targetLow.GlobalPosition);
		foot.LookAt(foot.GlobalPosition + Vector3.Left);
		
		if (drawDebugs) DrawDebugs(line, mid, U, P, proj);
	}

	private void DrawDebugs(Vector3 line, Vector3 mid, Vector3 U, Vector3 P, Vector3 proj)
	{
		var torus = new TorusMesh();
		torus.SurfaceSetMaterial(0, red);
		// radius of intersection
		var d =  DIS / line.Length();
		var R = (float)Math.Sqrt(d * d - (1f / 4f)) * line.Length();
		torus.OuterRadius = R; // assumes equal bone lengths
		torus.InnerRadius = torus.OuterRadius-0.01f;
		debugTorus.GetChild<MeshInstance3D>(0).Mesh = torus;
		debugTorus.LookAtFromPosition(mid, targetUp.GlobalPosition);

		var cylinder = new CylinderMesh();
		cylinder.SurfaceSetMaterial(0, red);
		cylinder.BottomRadius = 0.01f;
		cylinder.TopRadius = 0.01f;
		cylinder.Height = line.Length();
		debugCylinderLine.GetChild<MeshInstance3D>(0).Mesh = cylinder;
		debugCylinderLine.GetChild<MeshInstance3D>(0).Position = new Vector3(0, 0, -line.Length()/2);
		debugCylinderLine.LookAtFromPosition(targetLow.Position, targetUp.Position);
		
		cylinder = new CylinderMesh();
		cylinder.SurfaceSetMaterial(0, red);
		cylinder.BottomRadius = 0.02f;
		cylinder.TopRadius = 0.02f;
		cylinder.Height = R;
		debugCylinderU.GetChild<MeshInstance3D>(0).Mesh = cylinder;
		debugCylinderU.GetChild<MeshInstance3D>(0).Position = new Vector3(0, 0, -R/2);
		debugCylinderU.LookAtFromPosition(mid, mid+U.Normalized());
		
		cylinder = new CylinderMesh();
		cylinder.SurfaceSetMaterial(0, blue);
		cylinder.BottomRadius = 0.02f;
		cylinder.TopRadius = 0.02f;
		cylinder.Height = P.Length();
		debugCylinderP.GetChild<MeshInstance3D>(0).Mesh = cylinder;
		debugCylinderP.GetChild<MeshInstance3D>(0).Position = new Vector3(0, 0, -P.Length()/2);
		debugCylinderP.LookAtFromPosition(mid, pole.GlobalPosition);
		
		cylinder = new CylinderMesh();
		cylinder.SurfaceSetMaterial(0, yellow);
		cylinder.BottomRadius = 0.03f;
		cylinder.TopRadius = 0.03f;
		cylinder.Height = R;
		debugCylinderProj.GetChild<MeshInstance3D>(0).Mesh = cylinder;
		debugCylinderProj.GetChild<MeshInstance3D>(0).Position = new Vector3(0, 0, -R/2);
		debugCylinderProj.LookAtFromPosition(mid, mid+proj.Normalized()*R);
	}

	private Node3D isGrabbed = null;
	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton)
		{
			if (@event.IsPressed() && mouseButton.ButtonIndex == MouseButton.Left)
			{
				var space = GetWorld3D().DirectSpaceState;
				var mousePos = GetViewport().GetMousePosition();
				var start = GetViewport().GetCamera3D().ProjectRayOrigin(mousePos);
				var end = GetViewport().GetCamera3D().ProjectRayNormal(mousePos) * 100;
				var param = new PhysicsRayQueryParameters3D();
				
				param.From = start;
				param.To = end;
				
				var result = space.IntersectRay(param);

				if (result.ContainsKey("collider"))
				{
					if (result["collider"].ToString().Contains("TargetUp")) isGrabbed = targetUp;
					else if (result["collider"].ToString().Contains("TargetLow")) isGrabbed = targetLow;
					else if (result["collider"].ToString().Contains("Pole")) isGrabbed = pole;
					else isGrabbed = null;
				} else isGrabbed = null;
				
			}
		}
	}

	IEnumerable Rotate(Node3D bone, Vector3 goal)
	{
		Vector3 direction = (goal - bone.Rotation).Normalized();
		while (bone.Rotation != goal)
		{
			bone.Rotation += Math.Min(speed, (goal - bone.Rotation).Length()) * direction;
			yield return null;
		}
	}
	
	public static async void StartCoroutine(IEnumerable objects)
	{
		var mainLoopTree = Engine.GetMainLoop();
		foreach (var _ in objects)
		{
			await mainLoopTree.ToSignal(mainLoopTree, SceneTree.SignalName.ProcessFrame);
		}
	}
}
