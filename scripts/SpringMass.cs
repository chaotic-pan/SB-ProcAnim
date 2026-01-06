using System.Collections.Generic;
using Godot;

public partial class SpringMass : MeshInstance3D
{ 
	[Export] Mesh refMesh;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, Mesh.SurfaceGetArrays(0));
		var mdt = new MeshDataTool();
		mdt.CreateFromSurface(mesh, 0);
		GD.Print(mdt.GetFaceCount());
		for (var i = 0; i < mdt.GetVertexCount(); i++)
		{
			Vector3 vertex = mdt.GetVertex(i);
			if (vertex.Y > 0)
			{
				GD.Print(vertex);
				vertex.X = 0;
				vertex.Z = 0;
			}
			
			mdt.SetVertex(i, vertex);
		}
		mesh.ClearSurfaces();
		mdt.CommitToSurface(mesh);
		Mesh = mesh;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// var vertices = mesh.SurfaceGetArrays(0)[0];
	
	}
}
