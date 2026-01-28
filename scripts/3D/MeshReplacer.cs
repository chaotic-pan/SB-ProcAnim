using System.Collections.Generic;
using Godot;

public partial class MeshReplacer : Node3D
{
    private ImmediateMesh mesh;
    private SoftMesh softMesh;
    public void Init(SoftMesh softMesh)
    {
        GD.Print($"hii form {GetName()}");
        mesh = new ImmediateMesh();
        GetChild<MeshInstance3D>(0).Mesh = mesh;
        // var mat = new StandardMaterial3D();
        // mat.VertexColorUseAsAlbedo = true;
        // mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        // mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        // mat.AlbedoColor = new Color(1, 1, 1, 0.5f);
        // MaterialOverride = mat;
        this.softMesh = softMesh;
        drawMesh();

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
            var n = softMesh.internalVerts[0].position-((a+b+c)/3);
            mesh.SurfaceSetNormal(n);
		        
            if (face.Length == 4)
            {
                var d = softMesh.verts[face[3]].position;
                n = softMesh.internalVerts[0].position-((a+c)/2);
                mesh.SurfaceSetNormal(n);
                mesh.SurfaceAddVertex(ToLocal(d));
                mesh.SurfaceAddVertex(ToLocal(c));
                mesh.SurfaceAddVertex(ToLocal(a));
            }
		    	
            mesh.SurfaceAddVertex(ToLocal(c));
            mesh.SurfaceAddVertex(ToLocal(b));
            mesh.SurfaceAddVertex(ToLocal(a));
        }
		
        mesh.SurfaceEnd();
    }

}