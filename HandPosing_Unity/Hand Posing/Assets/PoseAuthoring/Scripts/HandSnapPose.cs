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
    public struct HandSnapPose
    {
        public Vector3 relativeGripPos;
        public Quaternion relativeGripRot;
        public bool isRightHand;

        [SerializeField]
        private List<BoneRotation> _bones;
        public List<BoneRotation> Bones
        {
            get
            {
                if(_bones == null)
                {
                    _bones = new List<BoneRotation>();
                }
                return _bones;
            }
        }
    }

}