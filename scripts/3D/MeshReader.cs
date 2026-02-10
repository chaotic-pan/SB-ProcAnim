using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using Godot.Collections;
using FileAccess = Godot.FileAccess;

public class SoftMesh(string meshName, List<Vertex> externalVerts, List<Vertex> internalVerts, List<int[]> faces)
{
	public string meshName = meshName; 
	public List<Vertex> externalVerts = externalVerts; 
	public List<Vertex> internalVerts = internalVerts; 
	public List<int[]> faces = faces;
}

public partial class MeshReader : Node3D
{
	[Export] private Script addedScript;
	[Export(PropertyHint.Flags, "Mesh:1,Wires:2,Shear:4,Structure:8")] public int draw { get; set; }
	[Export] private bool groundCollision;
	[Export] private float gravity = 0.1f;
	[Export] private float springConst = 0.2f;
	[Export(PropertyHint.Enum, "Lizard, QuadBall, Box, Trapezoid, Sheet, Test")] public int meshType { get; set; }
	private static readonly NumberFormatInfo Ci = CultureInfo.InvariantCulture.NumberFormat;

	private Array<MeshReplacer> bones = [];
	private List<SoftMesh> objects = [];
	private SpringMass springMass;
	
	public override void _Ready()
	{
		var obj = meshType==0? "lizartRef" : meshType==1? "subcube" : meshType==2? "box" : meshType==3? "trapezoid" 
			: meshType==4? "sheet" : "test";
		var path = "res://assets/" +obj+ ".obj";
		ReadInMesh(FileAccess.Open(path, FileAccess.ModeFlags.Read));
		
		var boneAtts = FindChildren("", "BoneAttachment3D");
		for (int i = 0; i < boneAtts.Count; i++)
		{
			boneAtts[i].SetScript(addedScript);
			boneAtts[i].SetProcess(true);
			boneAtts[i].SetPhysicsProcess(true);
			
			var bone = boneAtts[i] as MeshReplacer;
			var name = bone.GetName();
			var mesh = objects.Find(mesh => mesh.meshName.Equals(name));
			bone.Init(mesh, gravity, springConst, draw, groundCollision);
			bones.Add(bone);
		}
	}

	public override void _Process(double delta)
	{
		foreach (var bone in bones)	
		{ 
			bone.draw = draw;
		}
	}

	private void ReadInMesh(FileAccess file)
	{
		var content = file.GetAsText().Split("\n", false);
		String meshName = null;
		var verts = new List<Vertex>();
		var faces = new List<int[]>();
		int reset = 1;
		var j = 0;
		foreach (string line in content)
		{
			// new object start 
			if (line.StartsWith("o "))
			{
				if (meshName != null)
				{
					ReadInObject(meshName, verts, faces);
					reset += verts.Count;
				}
				meshName = line.Substring(2).Replace(".", "_");
				verts = [];
				faces = [];
			}
			// vertices
			else if (line.StartsWith("v "))
			{
				var split = line.Split(' ');
				var pos = new Vector3(
					float.Parse(split[1], Ci),
					float.Parse(split[2], Ci),
					float.Parse(split[3], Ci)
				);
				var vert = new Vertex(pos);
				verts.Add(vert);
			}
			// faces
			else if (line.StartsWith("f "))
			{
				var split = line.Split(' ');
				// f 1/1/1 2/2/1 4/3/1 3/4/1 
				if (split.Length > 5) GD.PushError($"Mesh {meshName} contains N-gons");
				
				// add index of normal and verts
				var a = Int32.Parse(split[1].Split("/")[0])-reset;
				var b = Int32.Parse(split[2].Split("/")[0])-reset;
				var c = Int32.Parse(split[3].Split("/")[0])-reset;

				if (split.Length == 5) // quad
				{
					var d = Int32.Parse(split[4].Split("/")[0])-reset;
					AddFace(verts[a], verts[b], verts[c], verts[d]);
					faces.Add([a, b, c, d]);
				}
				else // triangle
				{
					if (split.Length > 5) GD.PushWarning($"Mesh {meshName} contains Triangles");
					AddFace(verts[a], verts[b], verts[c], null);
					faces.Add([a, b, c]);
				}
			}
		}
		ReadInObject(meshName, verts, faces);
	}

	private void ReadInObject(string meshName, List<Vertex> vertices, List<int[]> faces)
	{
		var externalVerts = new List<Vertex>(vertices);
		var internalVerts = new List<Vertex>();
		
		var center = Subdiv(externalVerts);
		internalVerts.Add(center);
		
		foreach (var quad in faces.Where(face=> face.Length == 4))
		{
			// shear springs
			var a = externalVerts[quad[0]];
			var b = externalVerts[quad[1]];
			var c = externalVerts[quad[2]];
			var d = externalVerts[quad[3]];
			ConnectSpring(a, c, Springs.shear);
			ConnectSpring(b, d, Springs.shear);
			
			var newVert = Subdiv([a,b,c,d, center]);
			internalVerts.Add(newVert);
		}
		
		objects.Add(new SoftMesh(meshName, externalVerts, internalVerts, faces));
	}

	private Vertex Subdiv(List<Vertex> section)
	{
		// get center point
		var center = new Vertex(section);
		center.pin = true;

		// connect to all verts
		foreach (var vertex in section)
		{
			ConnectSpring(vertex, center, Springs.structure);
		}

		return center;
	}
	
	private void AddFace(Vertex a, Vertex b, Vertex c, Vertex d)
	{
		// add vert as each others neighbors + default distance 
		ConnectSpring(a, b, Springs.neighbour);
		ConnectSpring(b, c, Springs.neighbour);
		if (d==null)
		{
			ConnectSpring(c, a, Springs.neighbour);
		}
		else
		{
			ConnectSpring(c, d, Springs.neighbour);
			ConnectSpring(d, a, Springs.neighbour);
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
	
	

	
}


