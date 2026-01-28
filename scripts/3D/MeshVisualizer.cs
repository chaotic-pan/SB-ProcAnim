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
					
				var a = ToLocal(vertA.position);
				var b = ToLocal(vertB.Key.position);
				var dif = (a-b).Length() - vertB.Value;
				
				if (dif > vertB.Value*0.1) mesh.SurfaceSetColor(color.Darkened(0.3f));
				else if (dif < vertB.Value*-0.1) mesh.SurfaceSetColor(color.Lightened(0.3f));
				else mesh.SurfaceSetColor(color);
					
				mesh.SurfaceAddVertex(a);
				mesh.SurfaceAddVertex(b);
			}
		}
		mesh.SurfaceEnd();
	}

	public void drawMesh(List<int[]> faces, List<Vertex> verts, Vertex center)
	{
		mesh.ClearSurfaces();
		mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
		foreach (int[] face in faces)
		{
			var a = verts[face[0]].position;
			var b = verts[face[1]].position;
			var c = verts[face[2]].position;
			var n = center.position-((a+b+c)/3);
			mesh.SurfaceSetNormal(n);
		        
			if (face.Length == 4)
			{
				var d = verts[face[3]].position;
				n = center.position-((a+c)/2);
				mesh.SurfaceSetNormal(n);
				mesh.SurfaceAddVertex(ToLocal(d));
				mesh.SurfaceAddVertex(ToLocal(c));
				mesh.SurfaceAddVertex(ToLocal(a));
			}
		    	
			mesh.SurfaceAddVertex(ToLocal(c));
			mesh.SurfaceAddVertex(ToLocal(b));
			mesh.SurfaceAddVertex(ToLocal(a));
		}
		
		mesh.SurfaceEnd();
	}
	
	public void clear()
	{
		mesh.ClearSurfaces();
	}
}
