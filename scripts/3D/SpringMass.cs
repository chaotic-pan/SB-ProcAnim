using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;
using FileAccess = Godot.FileAccess;

public partial class SpringMass : MeshInstance3D
{ 
	[Export] private Mesh refMesh;
	private List<Vertex> verts = new();
	private List<Vector3> normals = new();
	private List<int[]> faces = new();
	private  NumberFormatInfo CI  = CultureInfo.InvariantCulture.NumberFormat;
	
	public override void _Ready()
	{
		ReadInMesh(FileAccess.Open(refMesh.GetPath(), FileAccess.ModeFlags.Read));
		
		BuildMesh();
	}

	private void ReadInMesh(FileAccess file)
	{
		var content = file.GetAsText().Split("\n", false);

		
		foreach (string line in content)
		{
			if (line.StartsWith("v "))
			{
				var split = line.Split(' ');
				verts.Add(new Vertex(new Vector3(
					float.Parse(split[1],CI),
					float.Parse(split[2],CI),
					float.Parse(split[3],CI)
				)));
			}
			if (line.StartsWith("vn"))
			{
				var split = line.Split(' ');
				normals.Add(new Vector3(
					float.Parse(split[1],CI),
					float.Parse(split[2],CI),
					float.Parse(split[3],CI)
				));
			}
			else if (line.StartsWith("f "))
			{
				var split = line.Split(' ');
				if (split.Length > 5) GD.PushWarning("Mesh contains N-gons");
				// f 1/1/1 2/2/1 4/3/1 3/4/1 
				var n = Int32.Parse(split[1].Split("/")[2])-1;
				var a = Int32.Parse(split[1].Split("/")[0])-1;
				var b = Int32.Parse(split[2].Split("/")[0])-1;
				var c = Int32.Parse(split[3].Split("/")[0])-1;
				
				faces.Add([n, a, b, c]);
				
				var ab = (verts[b].position - verts[a].position).Length();
				var ac = (verts[c].position - verts[a].position).Length();
				var bc = (verts[c].position - verts[b].position).Length();
				verts[a].neighbors.TryAdd(b, ab);
				verts[a].neighbors.TryAdd(c, ac);
				verts[b].neighbors.TryAdd(a, ab);
				verts[b].neighbors.TryAdd(c, bc);
				verts[c].neighbors.TryAdd(a, ac);
				verts[c].neighbors.TryAdd(b, bc);
				
				// if QUAD split into TRIS
				if (split.Length == 5)
				{
					var d = Int32.Parse(split[4].Split("/")[0])-1;
					faces.Add([n, a, c, d]);
					
					var ad = (verts[b].position - verts[a].position).Length();
					var cd = (verts[b].position - verts[a].position).Length();
					verts[a].neighbors.TryAdd(d, ad);
					verts[c].neighbors.TryAdd(d, cd);
					verts[d].neighbors.TryAdd(a, ad);
					verts[d].neighbors.TryAdd(c, cd);
				}
			}
			
		}
	}

	private void BuildMesh()
	{
		ImmediateMesh m = Mesh as ImmediateMesh;
		m.ClearSurfaces();
		m.SurfaceBegin(Mesh.PrimitiveType.Triangles);
		foreach (int[] face in faces)
		{
			var s = verts[face[3]].position;
			var r = verts[face[2]].position;
			var q = verts[face[1]].position;
			// m.SurfaceSetNormal(normals[face[0]]);
			m.SurfaceSetNormal((r-q).Cross(s-q));
			m.SurfaceAddVertex(s);
			m.SurfaceAddVertex(r);
			m.SurfaceAddVertex(q);
		}
		
		m.SurfaceEnd();
	}
	
	private Vector3 G = (new Vector3(0, -9.81f, 0))/10;
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		for (int i = 0; i < verts.Count; i++)
		{
			var v = ToGlobal(verts[i].position);
			// apply forces
			v += G * (float)delta; //Gravity
			
			// constraints
			if (v.Y <= 0) v.Y = 0; // ground plane collision
			
			verts[i].position = ToLocal(v);
		}	
		 /* SPRINGS
		foreach (var vert in verts)
		{
			foreach (KeyValuePair<int, float> neighbor in vert.neighbors)
			{
				var edge  = vert.position - verts[neighbor.Key].position;
				var dif = Math.Abs(edge.Length() - neighbor.Value);
				if (dif > neighbor.Value/2)
				{
					vert.position += edge * (dif/2);
					verts[neighbor.Key].position -= edge * (dif/2);
				}
			}
		}
		*/
		
		BuildMesh();
	}
}

class Vertex
{
	public Vector3 position;
	public Dictionary<int, float> neighbors = new ();
	public Dictionary<int, float> flexions = new();
	
	public Vertex(Vector3 position)
	{
		this.position = position;
	}
	
}
