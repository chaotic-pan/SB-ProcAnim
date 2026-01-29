using System.Collections.Generic;
using Godot;

public partial class MeshReplacer : Node3D
{
    private ImmediateMesh mesh;
    private SoftMesh softMesh;
    public void Init(SoftMesh softMesh)
    {
        mesh = new ImmediateMesh();
        var meshNode = GetChild<MeshInstance3D>(0);
        meshNode.Mesh = mesh;
        this.softMesh = softMesh;
        drawMesh();
        meshNode.GlobalPosition = Vector3.Zero;
        meshNode.GlobalRotation = Vector3.Zero;
        meshNode.Scale = Vector3.One;
    }
    
    public void drawMesh()
    {
        mesh.ClearSurfaces();
        mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
        
        foreach (int[] face in softMesh.faces)
        {
            var a = softMesh.verts[face[0]].position;
            var b = softMesh.verts[face[1]].position;
            var c = softMesh.verts[face[2]].position;
               var d = softMesh.verts[face[3]].position;
               
                
            var n = -(c-d).Cross(a-d);
                
            mesh.SurfaceSetNormal(n);
		        
            // if (face.Length == 4)
            // {
            //     mesh.SurfaceSetNormal(n);
            // }

            
            a = convertCoord(a);
            b = convertCoord(b);
            c = convertCoord(c);
            d = convertCoord(d);
            
		    	
                mesh.SurfaceAddVertex(d);
                mesh.SurfaceAddVertex(c);
                mesh.SurfaceAddVertex(a);
            mesh.SurfaceAddVertex(c);
            mesh.SurfaceAddVertex(b);
            mesh.SurfaceAddVertex(a);
        }
		
        mesh.SurfaceEnd();
    }


    private Vector3 convertCoord(Vector3 coord)
    { 
        coord /= Scale; 
        return coord;
    }
}