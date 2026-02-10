using System.Collections.Generic;
using System.Numerics;
using Godot;
using Vector3 = Godot.Vector3;

public partial class MeshReplacer : Node3D
{
    public int draw;
    private bool groundCollision;
    private float springConst;
    private Vector3 G;
    private Vector3 lastPos;
    private Vector3 lastRot;
    private ImmediateMesh mesh;
    private SoftMesh softMesh;
	
    private MeshVisualizer meshVisualizer;
    private MeshVisualizer wireVisualizer;
    private MeshVisualizer shearMV;
    private MeshVisualizer structureMV;
    
    public void Init(SoftMesh softMesh, float gravity, float springConst, int draw, bool groundCollision)
    {
	    this.softMesh = softMesh;
        G = new Vector3(0, -gravity, 0);
        this.springConst = springConst;
        this.draw = draw;
        this.groundCollision = groundCollision;

        var attach = GetChild<MeshInstance3D>(0);
        attach.Mesh = null;
        // DEBUG 
	        var cylinder = new CylinderMesh();
	        attach.Mesh = cylinder;
	        cylinder.Height = 0.005f;
	        cylinder.BottomRadius = 0.0005f;
	        cylinder.TopRadius = 0.0005f;
	        attach.Position = new Vector3(0, 0.0025f, 0);
	        attach.Rotation = Vector3.Zero;
       
        lastPos = GlobalPosition;
        lastRot = GlobalRotation;
        meshVisualizer = initMeshVisualizer(attach);
        wireVisualizer = initMeshVisualizer(attach);
        shearMV = initMeshVisualizer(attach);
        structureMV = initMeshVisualizer(attach);

        // convert internal points to local
        foreach (var  vert in softMesh.internalVerts)
        {
	        vert.position /= Scale;
        }

        foreach (var vert in softMesh.externalVerts)
        {
	        if (vert.neighbors.Count == 0)
	        {
		        GD.Print(softMesh.meshName);
	        }
        }
        
        DrawMesh();
    }

    private MeshVisualizer initMeshVisualizer(MeshInstance3D attach)
    {
	    var MV = new MeshVisualizer();
	    attach.AddChild(MV);
	    MV.GlobalPosition = Vector3.Zero;
	    MV.GlobalRotation = Vector3.Zero;
	    return MV;
    }

    private void DrawMesh()
	{
		if ((draw&(1<<2)) != 0) shearMV.drawWires(softMesh.externalVerts, Springs.shear, Colors.Turquoise);
		else shearMV.clear();
		
		if ((draw&(1<<3)) != 0) structureMV.drawWires(softMesh.internalVerts, Springs.structure, Colors.Magenta);
		else structureMV.clear();
		
		if ((draw&(1<<1)) != 0) wireVisualizer.drawWires(softMesh.externalVerts, Springs.neighbour, Colors.White);
		else wireVisualizer.clear();
		
		if ((draw&(1<<0)) != 0) meshVisualizer.drawMesh(softMesh.faces, softMesh.externalVerts);
		else meshVisualizer.clear();
	}

	// public override void _Process(double delta)
	// {
	// 	if (Input.IsKeyPressed(Key.M))
	// 	{
	// 		for (int i = 0; i < softMesh.internalVerts.Count; i++)
	// 		{
	// 			var a = softMesh.internalVerts[i].position;
	// 			var b = a.Rotated(Vector3.Up, (float)delta);
	// 			softMesh.internalVerts[i].position = b;
	// 		}
	// 	}
	// }

	public override void _PhysicsProcess(double delta)
	{
		//TODO 
		// 1 update all internal verts to adjust for movement + rotation
		// 2 physics shit all outer verts
		// 3 spring constraints, damping, collision
		// 4 profit 
		
		foreach (var vert in softMesh.externalVerts)
		{
			if (groundCollision && vert.position.Y == 0) continue;
			
			// EULER	newPos = pos + velocity;	newVel = velocity + acceleration
			
			// SEMI IMPLICIT EULER	 newPos = pos + velocity + acceleration
			
			// VERLET INTEGRATION	 newPos = 2*pos - prevPos + acceleration
			var newPos = 2 * vert.position - vert.prevPos + G;
			
			// constraints
			if (groundCollision && newPos.Y <= 0) newPos.Y = 0; // ground plane collision
			
			vert.prevPos = vert.position;
			vert.position = newPos;
			
		}	
		
		
		// SPRINGS
		ConstrainSprings(softMesh.internalVerts, Springs.structure);
		DrawMesh();
		return;
		ConstrainSprings(softMesh.externalVerts, Springs.neighbour);
		ConstrainSprings(softMesh.externalVerts, Springs.shear);
		
		foreach (var vert in softMesh.externalVerts)
		{
			if (groundCollision && vert.position.Y == 0) continue;
			
			// DAMP
			var move = vert.position - vert.prevPos;
			move *= 0.9f;
			var newPos = vert.prevPos + move;
			
			// constraints
			if (groundCollision &&  newPos.Y <= 0) newPos.Y = 0; // ground plane collision
			
			vert.position = newPos;
		}
		
		DrawMesh();
		lastPos = GlobalPosition;
		lastRot = GlobalRotation;
	}

	private void ConstrainSprings(List<Vertex> vertices, Springs springType)
	{
		foreach (var vert in vertices)
		{
			var list = springType switch
			{
				Springs.neighbour => vert.neighbors,
				Springs.shear => vert.shear,
				_ => vert.structure
			};
			foreach (KeyValuePair<Vertex, float> neighbor in list)
			{
				var v = vert.pin? vert.position*Scale : vert.position;
				var w = neighbor.Key.pin? neighbor.Key.position*Scale : neighbor.Key.position;
			
				// TODO move halfValue form midpoint instead of dif from Pos 
				
				var edge  = v - w;
				var dif = edge.Length() - neighbor.Value;
				edge = edge.Normalized();
				// var defRate = Math.Min(1, Math.Abs(1-(dif/neighbor.Value)));
				
				float sp = (vert.pin || neighbor.Key.pin)? 1 : 2;  // if one pinned, move other double
				// if (springType == Springs.structure) sp *= 0.5f;
				if (!vert.pin) v -= edge * (dif*springConst/sp);
				if (!neighbor.Key.pin) w += edge * (dif*springConst/sp);
				
				// ground plane collision
				if (groundCollision && v.Y <= 0) v.Y = 0; 
				if (groundCollision && w.Y <= 0) w.Y = 0; 
				
				if (!vert.pin) vert.position = v;
				if (!neighbor.Key.pin) neighbor.Key.position = w;
			}
		}
	}
}