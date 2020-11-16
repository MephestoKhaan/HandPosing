using System.Collections.Generic;
using UnityEngine;

namespace PoseAuthoring
{
    public enum Handeness
    {
        Left, Right
    }

    [System.Serializable]
    public struct BoneRotation
    {
        public OVRSkeleton.BoneId boneID;
        public Quaternion rotation;
    }

    [System.Serializable]
    public struct HandPose
    {

        public Pose relativeGrip;
        public Handeness handeness;

        [SerializeField]
        private List<BoneRotation> _bones;
        public List<BoneRotation> Bones
        {
            get
            {
                if (_bones == null)
                {
                    _bones = new List<BoneRotation>();
                }
                return _bones;
            }
        }

        public Pose ToPose(Transform relativeTo)
        {
            Vector3 globalPosDesired = relativeTo.TransformPoint(relativeGrip.position);
            Quaternion globalRotDesired = relativeTo.rotation * relativeGrip.rotation;
            return new Pose(globalPosDesired, globalRotDesired);
        }

        public HandPose AdjustPose(Pose pose, Transform relativeTo)
        {
            HandPose snapPose = this;
            snapPose.relativeGrip.position = relativeTo.InverseTransformPoint(pose.position);
            snapPose.relativeGrip.rotation = Quaternion.Inverse(relativeTo.rotation) * pose.rotation;
            return snapPose;
        }
    }

}