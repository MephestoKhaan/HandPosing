using UnityEngine;

namespace PoseAuthoring
{
    [System.Serializable]
    public class BoneMap
    {
        public OVRSkeleton.BoneId id;
        public Transform transform;
        public Vector3 rotationOffset;
        public bool updatePosition;
    }
}