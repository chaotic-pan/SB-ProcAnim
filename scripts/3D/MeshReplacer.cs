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
    
    private MeshVisualizer wireVisualizer;
    private MeshVisualizer shearMV;
    private MeshVisualizer structureMV;
    
    public void Init(SoftMesh softMesh, Material material, float gravity, float springConst, int draw, bool groundCollision)
    {
	    this.softMesh = softMesh;
        G = new Vector3(0, -gravity, 0);
        this.springConst = springConst;
        this.draw = draw;
        this.groundCollision = groundCollision;

        // replace previous ArrayMesh with ImmediateMesh
        Node3D meshNode = GetChild(0) as Node3D;
        meshNode.Position = Vector3.Zero;
        meshNode.Rotation = Vector3.Zero;
        meshNode.Scale = Vector3.One;
        mesh = new ImmediateMesh();
        (meshNode as MeshInstance3D).Mesh = mesh;
        (meshNode as MeshInstance3D).MaterialOverride = material;
        
       
        lastPos = GlobalPosition;
        lastRot = GlobalRotation;
      
        wireVisualizer = initMeshVisualizer(meshNode);
        shearMV = initMeshVisualizer(meshNode);
        structureMV = initMeshVisualizer(meshNode);

        // convert internal points to local space, to have them move along with the bones
         foreach (var  vert in softMesh.InternalVerts)
         {
	         vert.Position = ToLocal(vert.Position);
         }

        DrawObjects();
    }

    private MeshVisualizer initMeshVisualizer(Node3D meshNode)
    {
	    var MV = new MeshVisualizer();
	    meshNode.AddChild(MV);
	    MV.Position = Vector3.Zero;
	    MV.Rotation = Vector3.Zero;
	    MV.Scale = Vector3.One;
	    return MV;
    }

    private void DrawObjects()
	{
		if ((draw&(1<<2)) != 0) shearMV.drawWires(softMesh.ExternalVerts, Springs.shear, Colors.Turquoise);
		else shearMV.clear();
		
		if ((draw&(1<<3)) != 0) structureMV.drawWires(softMesh.InternalVerts, Springs.structure, Colors.Magenta);
		else structureMV.clear();
		
		if ((draw&(1<<1)) != 0) wireVisualizer.drawWires(softMesh.ExternalVerts, Springs.neighbour, Colors.White);
		else wireVisualizer.clear();
		
		if ((draw&(1<<0)) != 0) constructMesh();
		else mesh.ClearSurfaces();
	}
    
	public void constructMesh()
	{
		mesh.ClearSurfaces();
		mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);

		foreach (int[] face in softMesh.Faces)
		{
			var a = ToLocal(softMesh.ExternalVerts[face[0]].Position);
			var b = ToLocal(softMesh.ExternalVerts[face[1]].Position);
			var c = ToLocal(softMesh.ExternalVerts[face[2]].Position);
			
			var n = (c-b).Cross(a-b);
			mesh.SurfaceSetNormal(n);
		        
			if (face.Length == 4)
			{
				var d = ToLocal(softMesh.ExternalVerts[face[3]].Position);
				mesh.SurfaceAddVertex(d);
				mesh.SurfaceAddVertex(c);
				mesh.SurfaceAddVertex(a);
			}
			
			mesh.SurfaceAddVertex(c);
			mesh.SurfaceAddVertex(b);
			mesh.SurfaceAddVertex(a);
		}
		
		mesh.SurfaceEnd();
	}

	public override void _PhysicsProcess(double delta)
	{
		
		foreach (var vert in softMesh.ExternalVerts)
		{
			if (groundCollision && vert.Position.Y == 0) continue;
			
			// EULER	newPos = pos + velocity;	newVel = velocity + acceleration
			
			// SEMI IMPLICIT EULER	 newPos = pos + velocity + acceleration
			
			// VERLET INTEGRATION	 newPos = 2*pos - prevPos + acceleration
			var newPos = 2 * vert.Position - vert.PrevPos + G;
			
			// constraints
			if (groundCollision && newPos.Y <= 0) newPos.Y = 0; // ground plane collision
			
			vert.PrevPos = vert.Position;
			vert.Position = newPos;
			
		}	
		
		// SPRINGS
		ConstrainSprings(softMesh.InternalVerts, Springs.structure);
		ConstrainSprings(softMesh.ExternalVerts, Springs.neighbour);
		ConstrainSprings(softMesh.ExternalVerts, Springs.shear);
		
		foreach (var vert in softMesh.ExternalVerts)
		{
			if (groundCollision && vert.Position.Y == 0) continue;
			
			// DAMP
			var move = vert.Position - vert.PrevPos;
			move *= 0.9f;
			var newPos = vert.PrevPos + move;
			
			// constraints
			if (groundCollision &&  newPos.Y <= 0) newPos.Y = 0; // ground plane collision
			
			vert.Position = newPos;
		}
		
		DrawObjects();
		lastPos = GlobalPosition;
		lastRot = GlobalRotation;
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
				var v = vert.Pin? ToGlobal(vert.Position) : vert.Position;
				var w = neighbor.Key.Pin? ToGlobal(neighbor.Key.Position) : neighbor.Key.Position;
			
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
				if (groundCollision && v.Y <= 0) v.Y = 0; 
				if (groundCollision && w.Y <= 0) w.Y = 0; 
				
				if (!vert.Pin) vert.Position = v;
				if (!neighbor.Key.Pin) neighbor.Key.Position = w;
			}
		}
	}
}