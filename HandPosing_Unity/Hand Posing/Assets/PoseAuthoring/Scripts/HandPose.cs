using OVRSimpleJSON;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PoseAuthoring
{
    [System.Serializable]
    public struct BoneRotation
    {
        public OVRSkeleton.BoneId boneID;
        public Quaternion rotation;
    }

    [System.Serializable]
    public class HandPose 
    {
        public List<BoneRotation> Bones = new List<BoneRotation>();
        public Vector3 handPosition;
        public Quaternion handRotation;
        public bool isRightHand;


        public static float Score(HandPose from, HandPose to, float maxDistance = 0.2f)
        {
            if(from.isRightHand != to.isRightHand)
            {
                return 0f;
            }

            float rotationDifference = Quaternion.Dot(from.handRotation, to.handRotation) * 0.5f + 0.5f;
            float positionDifference = 1f-Mathf.Clamp01(Vector3.Distance(from.handPosition, to.handPosition) / maxDistance);

            return rotationDifference * positionDifference;
        }

    }

}