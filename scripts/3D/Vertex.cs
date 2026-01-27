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