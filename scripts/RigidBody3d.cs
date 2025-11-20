using Godot;
using System;
using Vector2 = Godot.Vector2;
using Vector3 = Godot.Vector3;

public partial class RigidBody3d : RigidBody3D
{
	private bool isGrabbed = false;
	private Vector2 grabPos;
	private int zPos = 10;
	private float dist = 100;
	private float speed = 0.1f;

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{	if (isGrabbed)
		{
			Vector3 targetPosition = MouseToWorldPos();
			Position = targetPosition;
		}
		
		var pos = Position;
		if (Input.IsKeyPressed(Key.Up)) pos.Y += 0.01f;
		else if (Input.IsKeyPressed(Key.Down)) pos.Y -= 0.01f;
		else if (Input.IsKeyPressed(Key.Left)) pos.X -= 0.01f;
		else if (Input.IsKeyPressed(Key.Right)) pos.X += 0.01f;
		else return;
		
		Position = pos;
	}
	
	// public override void _IntegrateForces(PhysicsDirectBodyState3D state)
	// {
	// 	if (isGrabbed)
	// 	{
	// 		Vector3 targetPosition = MouseToWorldPos();
	// 		MoveTo(state, GlobalTransform, targetPosition);
	// 	}
	// }
	//
	// private void MoveTo(PhysicsDirectBodyState3D state, Transform3D currentTransform, Vector3 targetPosition)
	// {
	// 	Vector3 direction = (targetPosition - currentTransform.Origin).Normalized();
	// 	float localSpeed = Mathf.Clamp(speed, 0.0f, direction.Length());
	// 	if (direction.Length() > 1e-4)
	// 	{
	// 		ApplyForce(direction, Vector3.Zero);
	// 	}
	// }

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton)
		{
			if (@event.IsPressed() && mouseButton.ButtonIndex == MouseButton.Left)
			{
				isGrabbed = true;
				var mousePos = GetViewport().GetMousePosition();
				var start = GetViewport().GetCamera3D().ProjectRayOrigin(mousePos);
				dist = (start-Position).Length();
				Vector3 targetPosition = MouseToWorldPos();
				// ApplyImpulse(targetPosition, Vector3.Zero);
			}
			if (@event.IsReleased() && mouseButton.ButtonIndex == MouseButton.Left)
			{
				isGrabbed = false;
			}
		}
	}
	
	private Vector3 MouseToWorldPos()
	{
		var mousePos = GetViewport().GetMousePosition();
		var pos=  GetViewport().GetCamera3D().ProjectRayNormal(mousePos) * dist;
		if (pos.Y < 0.2f) pos.Y = 0.2f; 
		if (pos.Y > 4) pos.Y = 4; 
		// GD.Print(pos);
		return pos;
		// var space = GetWorld3D().DirectSpaceState;
		// var mousePos = GetViewport().GetMousePosition();
		// var start = GetViewport().GetCamera3D().ProjectRayOrigin(mousePos);
		// var end = GetViewport().GetCamera3D().ProjectRayNormal(mousePos) * Dist;
		// var param = new PhysicsRayQueryParameters3D();
		//
		// param.From = start;
		// param.To = end;
		//
		// var result = space.IntersectRay(param);
		// if (!result["collider"].ToString().Contains("StaticBody"))
		// {
		// 	Object grabbedObj = result["collider"];
		// 	GD.Print(grabbedObj);
		// 	
		// }
	}
}
