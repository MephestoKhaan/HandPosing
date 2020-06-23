using System.Collections.Generic;
using UnityEngine;

namespace PoseAuthoring
{
    [System.Serializable]
    public struct BoneRotation
    {
        public OVRSkeleton.BoneId boneID;
        public Quaternion rotation;
    }

    [System.Serializable]
    public class HandSnapPose
    {
        public Vector3 relativeGripPos;
        public Quaternion relativeGripRot;
        public bool isRightHand;
        public List<BoneRotation> Bones = new List<BoneRotation>();
    }

}