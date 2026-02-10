using Godot;
using System.Collections.Generic;

public partial class MeshVisualizer : MeshInstance3D
{
	private ImmediateMesh mesh;

	public override void _Ready()
	{
		mesh = new ImmediateMesh();
		Mesh = mesh;
		var mat = new StandardMaterial3D();
		mat.VertexColorUseAsAlbedo = true;
		mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
		mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
		mat.AlbedoColor = new Color(1, 1, 1, 0.5f);
		MaterialOverride = mat;
	}

	public void drawWires(List<Vertex> verts, Springs springType, Color color)
	{
		mesh.ClearSurfaces();
		mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
		var drawn = new HashSet<Vertex[]>();
		foreach (var vertA in verts)
		{
			var list = springType switch
			{
				Springs.neighbour => vertA.neighbors,
				Springs.shear => vertA.shear,
				_ => vertA.structure
			};
			
			foreach (var vertB in list)
			{
				Vertex[] edge = [vertA, vertB.Key];
				if (!drawn.Add(edge)) continue;

				var scale = GetParent<Node3D>().Scale;
				var a = vertA.pin? vertA.position/scale : ToLocal(vertA.position);
				var b = vertB.Key.pin? vertB.Key.position/scale : ToLocal(vertB.Key.position);
				b /= Scale;
				//CODE var a = ToLocal(vertA.position);
				// var b = ToLocal(vertB.Key.position);
			
				var dif = (a-b).Length() - vertB.Value;
				
				mesh.SurfaceSetColor(color);
					
				mesh.SurfaceAddVertex(a);
				mesh.SurfaceAddVertex(b);
			}
		}
		mesh.SurfaceEnd();
	}
	
	public void drawMesh(List<int[]> faces, List<Vertex> verts)
	{
		mesh.ClearSurfaces();
		mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);

		foreach (int[] face in faces)
		{
			var a = ToLocal(verts[face[0]].position);
			var b = ToLocal(verts[face[1]].position);
			var c = ToLocal(verts[face[2]].position);
			
			var n = -(c-b).Cross(a-b);
			mesh.SurfaceSetNormal(n);
		        
			if (face.Length == 4)
			{
				var d = ToLocal(verts[face[3]].position);
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
	public void clear()
	{
		mesh.ClearSurfaces();
	}
}
