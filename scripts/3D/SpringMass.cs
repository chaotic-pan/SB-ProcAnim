using Godot;
using System.Collections.Generic;

public partial class SpringMass(int draw, float gravity, float springConst, 
	List<Vertex> verts, List<Vertex> externalVerts, List<Vertex> internalVerts, List<int[]> faces) : Node3D
{
	public int draw = draw;
	
	private Vector3 G = new Vector3(0, -gravity, 0);
	private Vector3 lastPos;
	
	private MeshVisualizer meshVisualizer;
	private MeshVisualizer wireVisualizer;
	private MeshVisualizer shearMV;
	private MeshVisualizer structureMV;
	
	public override void _Ready()
	{
		lastPos = GlobalPosition;
		meshVisualizer = new MeshVisualizer();
		AddChild(meshVisualizer);
		wireVisualizer = new MeshVisualizer();
		AddChild(wireVisualizer);
		shearMV = new MeshVisualizer();
		AddChild(shearMV);
		structureMV = new MeshVisualizer();
		AddChild(structureMV);
		DrawMesh();
	}

	private void DrawMesh()
	{
		if ((draw&(1<<2)) != 0) shearMV.drawWires(externalVerts, Springs.shear, Colors.Turquoise);
		else shearMV.clear();
		
		if ((draw&(1<<3)) != 0) structureMV.drawWires(internalVerts, Springs.structure, Colors.Magenta);
		else structureMV.clear();
		
		if ((draw&(1<<1)) != 0) wireVisualizer.drawWires(externalVerts, Springs.neighbour, Colors.White);
		// if ((draw&(1<<0)) != 0) meshVisualizer.drawMesh(faces, externalVerts, internalVerts[0]);
	}
	
	public override void _PhysicsProcess(double delta) 
	{
		foreach (var vert in verts)
		{
			if (vert.Position.Y == 0) continue;
			if (vert.Pin)
			{
				var move = GlobalPosition - lastPos;
				vert.Position += move;
				continue;
			}
			
			// EULER	newPos = pos + velocity;	newVel = velocity + acceleration
			
			// SEMI IMPLICIT EULER	 newPos = pos + velocity + acceleration
			
			// VERLET INTEGRATION	 newPos = 2*pos - prevPos + acceleration
			var newPos = 2 * vert.Position - vert.PrevPos + G;
			
			// constraints
			if (newPos.Y <= 0) newPos.Y = 0; // ground plane collision
			
			vert.PrevPos = vert.Position;
			vert.Position = newPos;
			
		}	
		
		// SPRINGS
		ConstrainSprings(internalVerts, Springs.structure);
		ConstrainSprings(externalVerts, Springs.neighbour);
		ConstrainSprings(externalVerts, Springs.shear);
		
		foreach (var vert in verts)
		{
			if (vert.Pin) continue;
			if (vert.Position.Y == 0) continue;
			
			// DAMP
			var move = vert.Position - vert.PrevPos;
			move *= 0.9f;
			var newPos = vert.PrevPos + move;
			
			// constraints
			if (newPos.Y <= 0) newPos.Y = 0; // ground plane collision
			
			vert.Position = newPos;
			
		}
		
		DrawMesh();
		lastPos = GlobalPosition;
	}

	private void ConstrainSprings(List<Vertex> vertices, Springs springType)
	{
		foreach (var vert in vertices)
		{
			var list = springType switch
			{
				Springs.neighbour => vert.Neighbors,
				Springs.shear => vert.Shear,
				_ => vert.Structure
			};
			foreach (KeyValuePair<Vertex, float> neighbor in list)
			{
				var v = vert.Position;
				var w = neighbor.Key.Position;
			
				// TODO move halfValue form midpoint instead of dif from Pos 
				
				var edge  = v - w;
				var dif = edge.Length() - neighbor.Value;
				edge = edge.Normalized();
				// var defRate = Math.Min(1, Math.Abs(1-(dif/neighbor.Value)));
				
				float sp = (vert.Pin || neighbor.Key.Pin)? 1 : 2;  // if one pinned, move other double
				// if (springType == Springs.structure) sp *= 0.5f;
				if (!vert.Pin) v -= edge * (dif*springConst/sp);
				if (!neighbor.Key.Pin) w += edge * (dif*springConst/sp);
				// ground plane collision
				if (v.Y <= 0) v.Y = 0; 
				if (w.Y <= 0) w.Y = 0; 
				
				vert.Position = v;
				neighbor.Key.Position = w;
			}
		}
	}
}
