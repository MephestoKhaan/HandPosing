using UnityEngine;

namespace PoseAuthoring
{
    public static class Utils
    {
        public static void SetPose(this Transform transform, Pose pose)
        {
            transform.SetPositionAndRotation(pose.position, pose.rotation);
        }

        public static Pose RelativeOffset(this Transform to, Transform from)
        {
            return RelativeOffset(to, from.position, from.rotation);
        }

        public static Pose RelativeOffset(this Transform to, Vector3 fromPosition, Quaternion fromRotation)
        {
            Quaternion inverseTo = Quaternion.Inverse(to.rotation);
            Vector3 relativePosition = inverseTo * (fromPosition - to.position);
            Quaternion relativeRotation = inverseTo * fromRotation;

            return new Pose(relativePosition, relativeRotation);
        }

        public static Pose GlobalPose(this Transform reference, Pose offset)
        {
            return new Pose(
                reference.position + reference.rotation * offset.position,
                reference.rotation * offset.rotation);
        }

    }
}