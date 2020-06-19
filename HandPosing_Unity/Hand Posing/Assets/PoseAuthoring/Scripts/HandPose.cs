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
        public Vector3 relativeGripPos;
        public Quaternion relativeGripRot;
        public bool isRightHand;
        public List<BoneRotation> Bones = new List<BoneRotation>();

        public static float Score(HandPose from, HandPose to, Transform relativeTo, float maxDistance = 0.1f)
        {
            if(from.isRightHand != to.isRightHand)
            {
                return 0f;
            }

            Quaternion globalRotFrom = relativeTo.rotation * from.relativeGripRot;
            Quaternion globalRotTo = relativeTo.rotation * to.relativeGripRot;

            Vector3 globalPosFrom = relativeTo.TransformPoint(from.relativeGripPos);
            Vector3 globalPosTo = relativeTo.TransformPoint(to.relativeGripPos);

            float forwardDifference = Vector3.Dot(globalRotFrom * Vector3.forward, globalRotTo * Vector3.forward) *0.5f+0.5f;
            float upDifference = Vector3.Dot(globalRotFrom * Vector3.up, globalRotTo * Vector3.up) * 0.5f + 0.5f;
            float positionDifference =  1f-Mathf.Clamp01(Vector3.Distance(globalPosFrom, globalPosTo) / maxDistance);

            return forwardDifference * upDifference * positionDifference;
        }

    }

}