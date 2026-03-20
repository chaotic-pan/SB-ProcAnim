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
				Springs.neighbour => vertA.Neighbors,
				Springs.shear => vertA.Shear,
				_ => vertA.Structure
			};
			
			foreach (var vertB in list)
			{
				Vertex[] edge = [vertA, vertB.Key];
				if (!drawn.Add(edge)) continue;

				var scale = GetParent<Node3D>().Scale;
				var a = vertA.Pin? vertA.Position : ToLocal(vertA.Position);
				var b = vertB.Key.Pin? vertB.Key.Position : ToLocal(vertB.Key.Position);
				
			
				var dif = (a-b).Length() - vertB.Value;
				
				mesh.SurfaceSetColor(color);
					
				mesh.SurfaceAddVertex(a);
				mesh.SurfaceAddVertex(b);
			}
		}
		mesh.SurfaceEnd();
	}

	public void clear()
	{
		mesh.ClearSurfaces();
	}
}
