using System.Collections.Generic;
using System.Linq;
using Godot;

public enum Springs
{
	neighbour,
	shear,
	structure
}

public class Vertex
{
	public Vector3 Position;
	public Vector3 PrevPos;
	public readonly Dictionary<Vertex, float> Neighbors = new ();
	public readonly Dictionary<Vertex, float> Shear = new();
	public readonly Dictionary<Vertex, float> Structure = new();
	public bool Pin;

	public Vertex(Vector3 position)
	{
		this.Position = position;
		PrevPos = position;
	}
	
	public Vertex(List<Vertex> constructed)
	{
		Position = (constructed.Aggregate(Vector3.Zero, (current, v) => current + v.Position))/constructed.Count;
		PrevPos = Position;
	}
	
}