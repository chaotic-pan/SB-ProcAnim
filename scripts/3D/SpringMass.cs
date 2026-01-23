using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using FileAccess = Godot.FileAccess;

public partial class SpringMass : MeshInstance3D
{
	[Export(PropertyHint.Flags, "Wires:1,Shear:2,Structure:4")] public int draw { get; set; }
	[Export] private bool pinCenter;
	[Export] private float gravity = 0.1f;
	[Export] private float springConst = 0.2f;
	[Export(PropertyHint.Enum, "Ball, QuadBall, Box, Trapezoid, Sheet")] public int meshType { get; set; }
	private Mesh refMesh;
	private readonly List<Vertex> verts = [];
	private readonly List<Vertex> externalVerts = [];
	private readonly List<Vertex> internalVerts = [];
	private readonly List<int[]> faces = [];
	private static readonly NumberFormatInfo Ci = CultureInfo.InvariantCulture.NumberFormat;
	private ImmediateMesh mesh;
	private ImmediateMesh shearWires;
	private ImmediateMesh strucWires;
	private Vector3 G;
	
	public override void _Ready()
	{
		G = new Vector3(0, -gravity, 0);
		var obj = meshType==0? "ball" : meshType==1? "subcube" : meshType==2? "box" : meshType==3? "trapezoid" : "sheet";
		var path = "res://assets/" +obj+ ".obj";
		ReadInMesh(FileAccess.Open(path, FileAccess.ModeFlags.Read));
		mesh = Mesh as ImmediateMesh;
		shearWires = GetChild<MeshInstance3D>(0).Mesh as ImmediateMesh;
		strucWires = GetChild<MeshInstance3D>(2).Mesh as ImmediateMesh;
		
		DrawMesh();
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
				var pos = new Vector3(
					float.Parse(split[1], Ci),
					float.Parse(split[2], Ci),
					float.Parse(split[3], Ci)
				);
				var vert = new Vertex(ToGlobal(pos));
				verts.Add(vert);
				externalVerts.Add(vert);
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
		
		Subdiv(externalVerts);
		
		foreach (var quad in faces.Where(face=> face.Length == 5))
		{
			// shear springs
			var a = verts[quad[1]];
			var b = verts[quad[2]];
			var c = verts[quad[3]];
			var d = verts[quad[4]];
			ConnectSpring(a, c, Springs.shear);
			ConnectSpring(b, d, Springs.shear);
			
			Subdiv([a,b,c,d, internalVerts[0]]);
		}
	}

	private void Subdiv(List<Vertex> section)
	{
		// get center point
		var center = new Vertex(section);
		verts.Add(center);
		internalVerts.Add(center);
		center.pin = pinCenter;

		// connect to all verts
		foreach (var vertex in section)
		{
			ConnectSpring(vertex, center, Springs.structure);
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
			case Springs.structure:
				a.structure.TryAdd(b, ab);
				b.structure.TryAdd(a, ab);
				return;
		}
		
	}

	private void DrawMesh()
	{
		mesh.ClearSurfaces();
		shearWires.ClearSurfaces();
		if ((draw&(1<<1)) != 0) // draw shear
		{
			shearWires.SurfaceBegin(Mesh.PrimitiveType.Lines);
			var drawn = new HashSet<Vertex[]>();
			foreach (var vert in externalVerts)
			{
				var a = ToLocal(vert.position);
				foreach (KeyValuePair<Vertex, float> shear in vert.shear)
				{
					if (!drawn.Add([vert, shear.Key])) continue;
					var b = ToLocal(shear.Key.position);

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
		strucWires.ClearSurfaces();
		if ((draw&(1<<3)) != 0) // draw structure
		{
			strucWires.SurfaceBegin(Mesh.PrimitiveType.Lines);
			foreach (var vert in internalVerts)
			{
				foreach (KeyValuePair<Vertex, float> struc in vert.structure)
				{
					var a = ToLocal(vert.position);
					var b = ToLocal(struc.Key.position);
			
					var dif = (a - b).Length() - struc.Value;
					if (dif > struc.Value * springConst) strucWires.SurfaceSetColor(Colors.Indigo);
					else if (dif < struc.Value * -springConst) strucWires.SurfaceSetColor(Colors.HotPink);
					else strucWires.SurfaceSetColor(Colors.Magenta);
			
					strucWires.SurfaceAddVertex(a);
					strucWires.SurfaceAddVertex(b);
				}
			}
			strucWires.SurfaceEnd();
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
						var a = ToLocal(vert.position);
						var b = ToLocal(neigh.Key.position);
					
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
	            var n = internalVerts[0].position-((a+b+c)/3);
            	mesh.SurfaceSetNormal(n);
	            
	            if (face.Length == 5)
	            {
		            var d = verts[face[4]].position;
		            n = internalVerts[0].position-((a+c)/2);
		            mesh.SurfaceSetNormal(n);
		            mesh.SurfaceAddVertex(ToLocal(d));
		            mesh.SurfaceAddVertex(ToLocal(c));
		            mesh.SurfaceAddVertex(ToLocal(a));
	            }
            	
            	mesh.SurfaceAddVertex(ToLocal(c));
            	mesh.SurfaceAddVertex(ToLocal(b));
            	mesh.SurfaceAddVertex(ToLocal(a));
	            
	           
            }
		}
		
		mesh.SurfaceEnd();
	}
	
	public override void _PhysicsProcess(double delta) 
	{
		foreach (var vert in verts)
		{
			if (vert.pin) continue;
			if (vert.position.Y == 0) continue;
			
			// EULER	newPos = pos + velocity;	newVel = velocity + acceleration
			
			// SEMI IMPLICIT EULER	 newPos = pos + velocity + acceleration
			
			// VERLET INTEGRATION	 newPos = 2*pos - prevPos + acceleration
			var newPos = 2 * vert.position - vert.prevPos + G;
			
			// constraints
			if (newPos.Y <= 0) newPos.Y = 0; // ground plane collision
			
			vert.prevPos = vert.position;
			vert.position = newPos;
			
		}	
		
		// SPRINGS
		ConstrainSprings(internalVerts, Springs.structure);
		ConstrainSprings(externalVerts, Springs.neighbour);
		ConstrainSprings(externalVerts, Springs.shear);
		
		foreach (var vert in verts)
		{
			if (vert.pin) continue;
			if (vert.position.Y == 0) continue;
			
			// DAMP
			var move = vert.position - vert.prevPos;
			move *= 0.9f;
			var newPos = vert.prevPos + move;
			
			// constraints
			if (newPos.Y <= 0) newPos.Y = 0; // ground plane collision
			
			vert.position = newPos;
			
		}
		
		DrawMesh();
	}

	private void ConstrainSprings(List<Vertex> vertices, Springs springType)
	{
		foreach (var vert in vertices)
		{
			var list = springType==Springs.neighbour? vert.neighbors : 
											springType==Springs.shear? vert.shear : vert.structure;
			foreach (KeyValuePair<Vertex, float> neighbor in list)
			{
				var v = vert.position;
				var w = neighbor.Key.position;
			
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
				if (v.Y <= 0) v.Y = 0; 
				if (w.Y <= 0) w.Y = 0; 
				
				vert.position = v;
				neighbor.Key.position = w;
			}
		}
	}
}

internal enum Springs
{
	neighbour,
	shear,
	structure
}

class Vertex
{
	public Vector3 position;
	public Vector3 prevPos;
	public readonly Dictionary<Vertex, float> neighbors = new ();
	public readonly Dictionary<Vertex, float> shear = new();
	public readonly Dictionary<Vertex, float> structure = new();
	public bool pin;

	public Vertex(Vector3 position)
	{
		this.position = position;
		prevPos = position;
	}
	
	public Vertex(List<Vertex> constructed)
	{
		position = (constructed.Aggregate(Vector3.Zero, (current, v) => current + v.position))/constructed.Count;
		prevPos = position;
	}

}
