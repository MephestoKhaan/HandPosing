using System;
using System.Collections.Generic;
using UnityEngine;


namespace HandPosing.OVRIntegration
{
    public class AvatarGhostOVR
    {
        private Transform customPose;
        public IntPtr sdkAvatar = IntPtr.Zero;

        private void UpdateCustomPoses()
        {
            var pose = GetAvatarPoseData(customPose);

        }

        private ovrAvatarTransform[] GetAvatarPoseData(Transform poseRoot)
        {
            List<Transform> joints = new List<Transform>();
            GetJointsRecursive(poseRoot, ref joints);
            return ToOVRAvatarTransform(joints);
        }

        private ovrAvatarTransform[] ToOVRAvatarTransform(List<Transform> joints)
        {
            ovrAvatarTransform[] avatarTransforms = new ovrAvatarTransform[joints.Count];
            for (int i = 0; i < joints.Count; ++i)
            {
                Transform joint = joints[i];
                ovrAvatarTransform transform = OvrAvatar.CreateOvrAvatarTransform(joint.localPosition, joint.localRotation);
                if (transform.position != avatarTransforms[i].position 
                    || transform.orientation != avatarTransforms[i].orientation)
                {
                    avatarTransforms[i] = transform;
                }
            }
            return avatarTransforms;
        }

        private void GetJointsRecursive(Transform transform, ref List<Transform> joints)
        {
            joints.Add(transform);
            for (int i = 0; i < transform.childCount; ++i)
            {
                Transform child = transform.GetChild(i);
                GetJointsRecursive(child, ref joints);
            }
        }
    }
}