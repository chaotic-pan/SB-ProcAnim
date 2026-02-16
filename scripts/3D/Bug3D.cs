using System;
using System.Collections.Generic;
using Godot;

partial class LegIK (Node3D IkController, Node3D PoleController, Vector3 restPos, Quaternion restRot, float reach, int upBoneIdx, int endBoneIdx)
{
    public Node3D IkController = IkController;
    public Node3D PoleController = PoleController;
    public Vector3 restPos = restPos;
    public Quaternion restRot = restRot;
    public float reach = reach;
    public int upBoneIdx = upBoneIdx;
    public int endBoneIdx = endBoneIdx;
}
public partial class Bug3D: Node3D 
{
    private Skeleton3D skelli;
    private int HipBoneIdx;
    private List<LegIK> legs = [];
    // bone rot
    // child bone rot
    
    private Node3D debugCylinderLine;

    public void Init(Skeleton3D skelli, TwoBoneIK3D legIKs, Node3D parent)
    {
        this.skelli = skelli;
        HipBoneIdx = skelli.GetBoneParent(legIKs.GetRootBone(0));
        legs.Add(LegInit(0, legIKs));
        legs.Add(LegInit(1, legIKs));
        
        var rootPos = skelli.ToGlobal(skelli.GetBoneGlobalPose(0).Origin);
        var rootRot = skelli.ToGlobal(skelli.GetBoneGlobalPose(0).Basis.GetRotationQuaternion().GetEuler());
        // debug line
        var cylinder = new CylinderMesh();
        var n = new Node3D();
        var node = new MeshInstance3D();
        node.Mesh = cylinder;
        var mat = new StandardMaterial3D();
        mat.AlbedoColor = Colors.Red;
        node.MaterialOverride = mat;
        cylinder.BottomRadius = 0.07f;
        cylinder.TopRadius = 0.02f;
        cylinder.Height = 1f;
        parent.AddChild(n);
        n.AddChild(node);
        n.Rotation = rootRot;
        // node.Position = new Vector3(0, 0, -cylinder.Height / 2);
        n.GlobalPosition = rootPos;
        n.Scale = Vector3.One/100;
        debugCylinderLine = n;
        
    }

    private LegIK LegInit(int IkIdx, TwoBoneIK3D leg)
    { 
        var rootPos = skelli.ToGlobal(skelli.GetBoneGlobalPose(0).Origin);
        Node3D IkController = (Node3D)leg.GetNode(leg.GetTargetNode(IkIdx));
        Node3D PoleController = (Node3D)leg.GetNode(leg.GetPoleNode(IkIdx));
        Vector3 restPos = IkController.GlobalPosition-rootPos;
        Quaternion restRot = Quaternion.FromEuler(IkController.GlobalRotation);
        
        float reach = 0;
        var upBoneIdx= leg.GetRootBone(IkIdx);
        var endBoneIdx = leg.GetEndBone(IkIdx);
        var upPos = skelli.ToGlobal(skelli.GetBoneGlobalPose(upBoneIdx).Origin);
        var midPos = skelli.ToGlobal(skelli.GetBoneGlobalPose(leg.GetMiddleBone(IkIdx)).Origin);
        var endPos = skelli.ToGlobal(skelli.GetBoneGlobalPose(endBoneIdx).Origin);
        reach += (midPos-upPos).Length(); 
        reach += (endPos-midPos).Length();
        
        return new LegIK(IkController,PoleController,restPos,restRot,reach, upBoneIdx, endBoneIdx);
    }
    public void Update(double delta)
    {
        var rootPos = skelli.ToGlobal(skelli.GetBoneGlobalPose(0).Origin);
        var rootRot = skelli.GetBonePoseRotation(0).GetEuler();
                var rot = new Vector3(-rootRot.X,0, -rootRot.Z);
                // GD.Print(rootRot);
                // GD.Print(rot);   
        foreach (var leg in legs)
        {
            var upPos = skelli.ToGlobal(skelli.GetBoneGlobalPose(leg.upBoneIdx).Origin); 
            var endPos = leg.IkController.GlobalPosition; 
            var dist = (endPos - upPos).Length();
            // GD.Print($"{dist} / {leg.reach}");
            if (dist >= leg.reach)
            {
                // leg.IkController.GlobalPosition = rootPos + leg.restPos;
                // leg.IkController.GlobalPosition = (rootPos + leg.restPos.Rotated(rot.Normalized(), rot.Length())).Normalized();
            }
        }
        debugCylinderLine.Rotation = rot;
        
    }
}