using UnityEngine;

namespace PoseAuthoring
{
    [System.Serializable]
    public class BoneMap
    {
        public OVRSkeleton.BoneId id;
        public Transform transform;
        public Vector3 rotationOffset;

        public Quaternion RotationOffset
        {
            get
            {
                return Quaternion.Euler(rotationOffset);
            }
        }
    }

    [System.Serializable]
    public class HandMap
    {
        public OVRSkeleton.BoneId id;
        public Transform transform;
        public Vector3 rotationOffset;
        public Vector3 positionOffset;

        public Quaternion RotationOffset
        {
            get
            {
                return Quaternion.Euler(rotationOffset);
            }
        }
    }
}