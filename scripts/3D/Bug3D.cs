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
    public Skeleton3D skelli;
    public int HipBoneIdx;
    public List<LegIK> legs = [];
    // bone rot
    // child bone rot

    public void Init(Skeleton3D skelli, TwoBoneIK3D legIKs)
    {
        this.skelli = skelli;
        HipBoneIdx = skelli.GetBoneParent(legIKs.GetRootBone(0));
        legs.Add(LegInit(0, legIKs));
        legs.Add(LegInit(1, legIKs));
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
        var rootRot = skelli.ToGlobal(skelli.GetBoneGlobalPose(0).Basis.GetRotationQuaternion().GetEuler());
        foreach (var leg in legs)
        {
            var upPos = skelli.ToGlobal(skelli.GetBoneGlobalPose(leg.upBoneIdx).Origin); 
            var endPos = leg.IkController.GlobalPosition; 
            var dist = (endPos - upPos).Length();
            // GD.Print($"{dist} / {leg.reach}");
            if (dist >= leg.reach)
            {
                leg.IkController.GlobalPosition = rootPos + leg.restPos;
                // leg.IkController.GlobalPosition = rootPos + (leg.restPos.Rotated(rootRot.Normalized(), rootPos.Length()));
            }
        }
        
    }
}