using UnityEngine;

namespace PoseAuthoring
{
    public static class PoseUtils
    {
        public static void SetPose(this Transform transform, Pose pose, Space space = Space.World)
        {
            if(space == Space.World)
            {
                transform.SetPositionAndRotation(pose.position, pose.rotation);
            }
            else
            {
                transform.localRotation = pose.rotation;
                transform.localPosition = pose.position;
            }
        }

        public static Pose GetPose(this Transform transform, Space space = Space.World)
        {
            if (space == Space.World)
            {
                return new Pose(transform.position, transform.rotation);
            }
            else
            {
                return new Pose(transform.localPosition, transform.localRotation);
            }
        }

        public static Pose RelativeOffset(this Transform to, Transform from)
        {
            return RelativeOffset(from.position, from.rotation, to.position, to.rotation);
        }

        public static Pose RelativeOffset(this Transform to, Pose from)
        {
            return RelativeOffset(from.position, from.rotation, to.position, to.rotation);
        }

        public static Pose RelativeOffset(Pose from, Pose to)
        {
            return RelativeOffset(from.position, from.rotation, to.position, to.rotation);
        }

        public static Pose RelativeOffset(Vector3 fromPosition, Quaternion fromRotation, Vector3 toPosition, Quaternion toRotation)
        {
            Quaternion inverseTo = Quaternion.Inverse(toRotation);
            Vector3 relativePosition = inverseTo * (fromPosition - toPosition);
            Quaternion relativeRotation = inverseTo * fromRotation;

            return new Pose(relativePosition, relativeRotation);
        }

        public static Pose GlobalPose(this Transform reference, Pose offset)
        {
            return new Pose(
                reference.position + reference.rotation * offset.position,
                reference.rotation * offset.rotation);
        }


        public static float Similitude(Pose from, Pose to, float maxDistance)
        {
            float rotationDifference = RotationDifference(from.rotation, to.rotation);
            float positionDifference = 1f - Mathf.Clamp01(Vector3.Distance(from.position, to.position) / maxDistance);
            return rotationDifference  * positionDifference;
        }

        public static float RotationDifference(Quaternion from, Quaternion to)
        {
            float forwardDifference = Vector3.Dot(from * Vector3.forward, to * Vector3.forward) * 0.5f + 0.5f;
            float upDifference = Vector3.Dot(from * Vector3.up, to * Vector3.up) * 0.5f + 0.5f;
            return forwardDifference * upDifference;
        }
    }
}