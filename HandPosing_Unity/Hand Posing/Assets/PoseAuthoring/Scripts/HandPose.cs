using System;
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
    public class HandPose
    {
        public Vector3 handGripPosition;
        public Quaternion handGripRotation;
        public bool isRightHand;
        public List<BoneRotation> Bones = new List<BoneRotation>();

        public static float Score(HandPose from, HandPose to, float maxDistance = 0.2f)
        {
            if(from.isRightHand != to.isRightHand)
            {
                return 0f;
            }

            float rotationDifference = Math.Max(0f,Quaternion.Dot(from.handGripRotation, to.handGripRotation));
            float positionDifference = 1f-Mathf.Clamp01(Vector3.Distance(from.handGripPosition, to.handGripPosition) / maxDistance);

            return rotationDifference * positionDifference;
        }

    }

}