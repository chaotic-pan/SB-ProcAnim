using System.Collections.Generic;
using Godot;

public class SoftMesh(string meshName, List<Vertex> externalVerts, List<Vertex> internalVerts, List<int[]> faces)
{
	public readonly string MeshName = meshName; 
	public readonly List<Vertex> ExternalVerts = externalVerts; 
	public readonly List<Vertex> InternalVerts = internalVerts; 
	public readonly List<int[]> Faces = faces;
}
