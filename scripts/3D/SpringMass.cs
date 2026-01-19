using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using FileAccess = Godot.FileAccess;

public partial class SpringMass : MeshInstance3D
{ 
	[Export(PropertyHint.Flags, "Wires:1,Shear:2,Flexion:4")] public int draw { get; set; } = 0;
	[Export] private bool pinCenter;
	[Export] private float springConst = 0.2f;
	[Export(PropertyHint.Enum, "Ball, QuadBall, Box, Sheet")] public int meshType { get; set; }
	private Mesh refMesh;
	private readonly List<Vertex> verts = [];
	private readonly List<Vertex> externalVerts = [];
	private readonly List<Vertex> internalVerts = [];
	private readonly List<Vector3> normals = [];
	private readonly List<int[]> faces = [];
	private static readonly NumberFormatInfo Ci = CultureInfo.InvariantCulture.NumberFormat;
	private ImmediateMesh mesh;
	private ImmediateMesh shearWires;
	private ImmediateMesh flexWires;
	
	public override void _Ready()
	{
		var obj = meshType==0? "ball" : meshType==1? "lpBall" : meshType==2? "box" : "sheet";
		var path = "res://assets/" +obj+ ".obj";
		ReadInMesh(FileAccess.Open(path, FileAccess.ModeFlags.Read));
		mesh = Mesh as ImmediateMesh;
		shearWires = GetChild<MeshInstance3D>(0).Mesh as ImmediateMesh;
		flexWires = GetChild<MeshInstance3D>(1).Mesh as ImmediateMesh;
		
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
				var vert = new Vertex(new Vector3(
					float.Parse(split[1],Ci),
					float.Parse(split[2],Ci),
					float.Parse(split[3],Ci)
				));
				verts.Add(vert);
				externalVerts.Add(vert);
			}
			// normals
			else if (line.StartsWith("vn"))
			{
				var split = line.Split(' ');
				normals.Add(new Vector3(
					float.Parse(split[1],Ci),
					float.Parse(split[2],Ci),
					float.Parse(split[3],Ci)
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
				var d = split.Length == 5? Int32.Parse(split[4].Split("/")[0])-1 : -1;
				AddFace(n, a, b, c, d);
				
				if (pin)
				{
					verts[a].pin = true;
					verts[b].pin = true;
					verts[c].pin = true;
					if (d != -1) verts[d].pin = true;
				}
				
			}
		}

		// add center spring point
		var center = new Vertex(externalVerts);
		verts.Add(center);
		internalVerts.Add(center);
		center.pin = pinCenter;
		
		foreach (var quad in faces.Where(face=> face.Length == 5))
		{
			// shear springs
			var a = verts[quad[1]];
			var b = verts[quad[2]];
			var c = verts[quad[3]];
			var d = verts[quad[4]];
			ConnectSpring(a, c, Springs.shear);
			ConnectSpring(b, d, Springs.shear);
			
			//TODO face center points connected to center + corners
			// var e = new Vertex([a, b, c, d]);
			// verts.Add(e);
			// internalVerts.Add(e);
			// ConnectSpring(e, center, Springs.flexion);
			// ConnectSpring(e, a, Springs.flexion);
			// ConnectSpring(e, b, Springs.flexion);
			// ConnectSpring(e, c, Springs.flexion);
			// ConnectSpring(e, d, Springs.flexion);
		}
	}
	
	private void AddFace(int n, int a, int b, int c, int d)
	{
		if (d==-1) faces.Add([n, a, b, c]);
		else faces.Add([n, a, b, c, d]);
                    
		// add vert as each others neighbors + default distance 
		ConnectSpring(verts[a], verts[b], Springs.neighbour);
		ConnectSpring(verts[b], verts[c], Springs.neighbour);
		if (d==-1)
		{
			ConnectSpring(verts[c], verts[a], Springs.neighbour);
		}
		else
		{
			ConnectSpring(verts[c], verts[d], Springs.neighbour);
			ConnectSpring(verts[d], verts[a], Springs.neighbour);
		}
	}
	
	private void ConnectSpring(Vertex a, Vertex b, Springs springType)
	{
		if (a.neighbors.ContainsKey(b)) return;
		
		var ab = (b.position - a.position).Length();
		switch (springType)
		{
			case Springs.neighbour:
				a.neighbors.TryAdd(b, ab);
				b.neighbors.TryAdd(a, ab);
				return;
			case Springs.shear:
				a.shear.TryAdd(b, ab);
				b.shear.TryAdd(a, ab);
				return;
			case Springs.flexion:
				a.flexion.TryAdd(b, ab);
				b.flexion.TryAdd(a, ab);
				return;
		}
		
	}

	private void BuildMesh()
	{
		mesh.ClearSurfaces();
		shearWires.ClearSurfaces();
		if ((draw&(1<<1)) != 0) // draw shear
		{
			shearWires.SurfaceBegin(Mesh.PrimitiveType.Lines);
			var drawn = new HashSet<Vertex[]>();
			foreach (var vert in externalVerts)
			{
				var a = vert.position;
				foreach (KeyValuePair<Vertex, float> shear in vert.shear)
				{
					if (!drawn.Add([vert, shear.Key])) continue;
					var b = shear.Key.position;

					var dif = (a - b).Length() - shear.Value;
					if (dif > shear.Value * springConst) shearWires.SurfaceSetColor(Colors.Chartreuse);
					else if (dif < shear.Value * -springConst) shearWires.SurfaceSetColor(Colors.RoyalBlue);
					else shearWires.SurfaceSetColor(Colors.Turquoise);

					shearWires.SurfaceAddVertex(a);
					shearWires.SurfaceAddVertex(b);
				}
			}
			shearWires.SurfaceEnd();
		}
		flexWires.ClearSurfaces();
		if ((draw&(1<<2)) != 0) // draw flexion
		{
			flexWires.SurfaceBegin(Mesh.PrimitiveType.Lines);
			foreach (var vert in internalVerts)
			{
				foreach (KeyValuePair<Vertex, float> flex in vert.flexion)
				{
					var a = vert.position;
					var b = flex.Key.position;

					var dif = (a - b).Length() - flex.Value;
					if (dif > flex.Value * springConst) flexWires.SurfaceSetColor(Colors.Orange);
					else if (dif < flex.Value * -springConst) flexWires.SurfaceSetColor(Colors.LimeGreen);
					else flexWires.SurfaceSetColor(Colors.Yellow);

					flexWires.SurfaceAddVertex(a);
					flexWires.SurfaceAddVertex(b);
				}
			}
			flexWires.SurfaceEnd();
		}
		if ((draw&(1<<0)) != 0) // draw wireframe
		{
			mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
			var drawn = new HashSet<Vertex[]>();
			foreach (var vert in externalVerts)
			{
				foreach (var neigh in vert.neighbors)
				{
					Vertex[] edge = [vert, neigh.Key];
					if (drawn.Add(edge))
					{
						var a = vert.position;
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
		else // draw mesh
		{ 
			mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
            foreach (int[] face in faces)
            {
	            var a = verts[face[1]].position;
            	var b = verts[face[2]].position;
            	var c = verts[face[3]].position;
            	// m.SurfaceSetNormal(normals[face[0]]);
            	mesh.SurfaceSetNormal((b-c).Cross(a-c));
            	mesh.SurfaceAddVertex(a);
            	mesh.SurfaceAddVertex(b);
            	mesh.SurfaceAddVertex(c);
	            
	            if (face.Length == 5)
	            {
		            var d = verts[face[4]].position;
		            mesh.SurfaceAddVertex(a);
		            mesh.SurfaceAddVertex(c);
		            mesh.SurfaceAddVertex(d);
	            }
            }
		}
		
		mesh.SurfaceEnd();
	}
	
	private Vector3 G = (new Vector3(0, -9.81f, 0))/10;
	public override void _PhysicsProcess(double delta)
	{
		foreach (var vert in verts)
		{
			if (vert.pin) continue;
			var v = ToGlobal(vert.position);
			// apply forces
			v += G * (float)delta; //Gravity
			
			// constraints
			if (v.Y <= 0) v.Y = 0; // ground plane collision
			
			vert.position = ToLocal(v);
		}	
		
		//TODO pull face centers back to face 
		// foreach (var vert in internalVerts)
		// {
		// 	if (vert.pin) continue;
		// 	
		// 	var v = ToGlobal(vert.position);
		// 	var w = ToGlobal(vert.getConPos());
		// 	
		// 	var edge  = v - w;
		// 	var dif = edge.Length();
		// 	edge = edge.Normalized();
		// 	
		// 	v -= edge * (dif*springConst*2);
		// 	// ground plane collision
		// 	if (v.Y <= 0) v.Y = 0; 
		// 		
		// 	vert.position = ToLocal(v);
		// }
		
		 // SPRINGS
		//TODO ConstrainSprings(internalVerts, Springs.flexion);
		ConstrainSprings(externalVerts, Springs.neighbour);
		ConstrainSprings(externalVerts, Springs.shear);

		BuildMesh();
	}

	private void ConstrainSprings(List<Vertex> vertices, Springs springType)
	{
		foreach (var vert in vertices)
		{
			var list = springType==Springs.neighbour? vert.neighbors : 
											springType==Springs.shear? vert.shear : vert.flexion;
			foreach (KeyValuePair<Vertex, float> neighbor in list)
			{
				var v = ToGlobal(vert.position);
				var w = ToGlobal(neighbor.Key.position);
			
				var edge  = v - w;
				var dif = edge.Length() - neighbor.Value;
				edge = edge.Normalized();
				
				var sp = (!vert.pin || !neighbor.Key.pin)? springConst * 2 : springConst;
				if (!vert.pin) v -= edge * (dif*sp);
				if (!neighbor.Key.pin) w += edge * (dif*sp);
				// ground plane collision
				if (v.Y <= 0) v.Y = 0; 
				if (w.Y <= 0) w.Y = 0; 
				
				vert.position = ToLocal(v);
				neighbor.Key.position = ToLocal(w);
			}
		}
	}
}

internal enum Springs
{
	neighbour,
	shear,
	flexion
}

class Vertex
{
	public Vector3 position;
	public readonly Dictionary<Vertex, float> neighbors = new ();
	public readonly Dictionary<Vertex, float> shear = new();
	public readonly Dictionary<Vertex, float> flexion = new();
	public readonly List<Vertex> constructed;
	public bool pin;

	public Vertex(Vector3 position)
	{
		this.position = position;
	}
	
	public Vertex(List<Vertex> constructed)
	{
		this.constructed = constructed;
		position = getConPos();
	}
	
	public Vector3 getConPos()
	{
		var conPos  = constructed.Aggregate(Vector3.Zero, (current, v) => current + v.position);
		return conPos/constructed.Count;
	}

}
