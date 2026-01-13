using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;
using FileAccess = Godot.FileAccess;

public partial class SpringMass : MeshInstance3D
{ 
	[Export] private bool drawAsWireframe = false;
	[Export] private bool drawFlexionWireframe = false;
	[Export] private float springConst = 0.2f;
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
		bool pin = false;
		
		foreach (string line in content)
		{
			// vertices
			if (line.StartsWith("v "))
			{
				var split = line.Split(' ');
				verts.Add(new Vertex(new Vector3(
					float.Parse(split[1],CI),
					float.Parse(split[2],CI),
					float.Parse(split[3],CI)
				)));
			}
			// normals
			else if (line.StartsWith("vn"))
			{
				var split = line.Split(' ');
				normals.Add(new Vector3(
					float.Parse(split[1],CI),
					float.Parse(split[2],CI),
					float.Parse(split[3],CI)
				));
			}
			else if (line.StartsWith("g"))
			{
				if (line.Contains("pin")) pin = true;
				if (line.Contains("free")) pin = false;
			}
			// faces
			else if (line.StartsWith("f "))
			{
				var split = line.Split(' ');
				// f 1/1/1 2/2/1 4/3/1 3/4/1 
				if (split.Length > 5) GD.PushWarning("Mesh contains N-gons");
				// add index of normal and verts
				var n = Int32.Parse(split[1].Split("/")[2])-1;
				var a = Int32.Parse(split[1].Split("/")[0])-1;
				var b = Int32.Parse(split[2].Split("/")[0])-1;
				var c = Int32.Parse(split[3].Split("/")[0])-1;
				faces.Add([n, a, b, c]);
				
				// add vert as each others neighbors + default distance 
				ConnectSpring(verts[a], verts[b], false);
				ConnectSpring(verts[a], verts[c], false);
				ConnectSpring(verts[b], verts[c], false);

				if (pin)
				{
					verts[a].pin = true;
					verts[b].pin = true;
					verts[c].pin = true;
				}
				
				// if QUAD split into TRIS
				if (split.Length == 5)
				{
					var d = Int32.Parse(split[4].Split("/")[0])-1;
					faces.Add([n, a, c, d]);
					
					// ac already connected
					ConnectSpring(verts[a], verts[d], false);
					ConnectSpring(verts[c], verts[d], false);
					if (pin) verts[a].pin = true;
				}
			}
		}
		
		// add 2nd neighbors for flexion springs
		foreach (var vert in verts)
		{
			foreach (KeyValuePair<Vertex, float> neighbor in vert.neighbors) // for each neighbor
			{
				foreach (KeyValuePair<Vertex, float> flex in neighbor.Key.neighbors) // add each neighbor as 2nd
				{
					ConnectSpring(vert, flex.Key, true);
				}
			}
		}
	}
	
	private void ConnectSpring(Vertex a, Vertex b, bool flexion)
	{
		if (a.neighbors.ContainsKey(b)) return;
		
		var ab = (b.position - a.position).Length();
		if (flexion)
		{
			a.flexions.TryAdd(b, ab);
			b.flexions.TryAdd(a, ab);
			return;
		}
		a.neighbors.TryAdd(b, ab);
		b.neighbors.TryAdd(a, ab);
	}

	private void BuildMesh()
	{
		ImmediateMesh mesh = Mesh as ImmediateMesh;
		mesh.ClearSurfaces();
		if (drawFlexionWireframe)
		{
			ImmediateMesh flexWires = GetChild<MeshInstance3D>(0).Mesh as ImmediateMesh;
			flexWires.ClearSurfaces();
			flexWires.SurfaceBegin(Mesh.PrimitiveType.Lines);
			var drawn = new HashSet<Vertex[]>();
			for (var i = 0; i < verts.Count; i++)
			{
				foreach (var flex in verts[i].flexions)
				{
					Vertex[] edge = [verts[i], flex.Key];
					if (drawn.Add(edge))
					{
						var a = verts[i].position;
						var b = flex.Key.position;

						var dif = (a - b).Length() - flex.Value;
						if (dif > flex.Value * springConst) flexWires.SurfaceSetColor(Colors.Orange);
						else if (dif < flex.Value * -springConst) flexWires.SurfaceSetColor(Colors.LimeGreen);
						else flexWires.SurfaceSetColor(Colors.Yellow);

						flexWires.SurfaceAddVertex(a);
						flexWires.SurfaceAddVertex(b);
					}
				}
			}
			flexWires.SurfaceEnd();
		}
		if (drawAsWireframe)
		{
			mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
			var drawn = new HashSet<Vertex[]>();
			for (var i=0; i<verts.Count; i++)
			{
				foreach (var neigh in verts[i].neighbors)
				{
					Vertex[] edge = [verts[i], neigh.Key];
					if (drawn.Add(edge))
					{
						var a = verts[i].position;
						var b = neigh.Key.position;
					
						var dif = (a-b).Length() - neigh.Value;
						if (dif > neigh.Value*springConst) mesh.SurfaceSetColor(Colors.DarkRed);
						else if (dif < neigh.Value*-springConst) mesh.SurfaceSetColor(Colors.Green);
						else mesh.SurfaceSetColor(Colors.White);
					
						mesh.SurfaceAddVertex(a);
						mesh.SurfaceAddVertex(b);
					}
				}
			}
		}
		else
		{ 
			mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
            foreach (int[] face in faces)
            {
            	var a = verts[face[3]].position;
            	var b = verts[face[2]].position;
            	var c = verts[face[1]].position;
            	// m.SurfaceSetNormal(normals[face[0]]);
            	mesh.SurfaceSetNormal((b-c).Cross(a-c));
            	mesh.SurfaceAddVertex(a);
            	mesh.SurfaceAddVertex(b);
            	mesh.SurfaceAddVertex(c);
            }
		}
		
		mesh.SurfaceEnd();
	}
	
	private Vector3 G = (new Vector3(0, -9.81f, 0))/10;
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		for (int i = 0; i < verts.Count; i++)
		{
			if (verts[i].pin) continue;
			
			var v = ToGlobal(verts[i].position);
			// apply forces
			v += G * (float)delta; //Gravity
			
			// constraints
			if (v.Y <= 0) v.Y = 0; // ground plane collision
			
			verts[i].position = ToLocal(v);
		}	
		
		 // SPRINGS
		ConstrainSprings(false);
		ConstrainSprings(true);

		BuildMesh();
	}

	private void ConstrainSprings(bool flexions)
	{
		foreach (var vert in verts)
		{
			var list = flexions ? vert.flexions : vert.neighbors;
			foreach (KeyValuePair<Vertex, float> neighbor in list)
			{
				var v = ToGlobal(vert.position);
				var w = ToGlobal(neighbor.Key.position);
			
				var edge  = v - w;
				var dif = edge.Length() - neighbor.Value;
				edge = edge.Normalized();
				
					if (!vert.pin) v -= edge * (dif*springConst);
					if (!neighbor.Key.pin) w += edge * (dif*springConst);
					// ground plane collision
					if (v.Y <= 0) v.Y = 0; 
					if (w.Y <= 0) w.Y = 0; 
				
				vert.position = ToLocal(v);
				neighbor.Key.position = ToLocal(w);
			}
		}
	}
}


class Vertex
{
	public Vector3 position;
	public Dictionary<Vertex, float> neighbors = new ();
	public Dictionary<Vertex, float> flexions = new();
	public bool pin;
	
	public Vertex(Vector3 position)
	{
		this.position = position;
	}
	
}
