using UnityEngine;

namespace PoseAuthoring
{
    public static class Utils
    {
        public static void SetPose(this Transform transform, Pose pose)
        {
            transform.SetPositionAndRotation(pose.position, pose.rotation);
        }


    }
}