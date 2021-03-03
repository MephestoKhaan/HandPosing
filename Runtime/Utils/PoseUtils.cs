using UnityEngine;

namespace HandPosing
{
    /// <summary>
    /// Tools for working with Unity Poses
    /// </summary>
    public static class PoseUtils
    {
        /// <summary>
        /// Assings a Pose to a given transform.
        /// </summary>
        /// <param name="transform"> The transform to which apply the pose.</param>
        /// <param name="pose">The desired pose.</param>
        /// <param name="space">If the pose should be aplied to the loca position/rotation or world position/rotation.</param>
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

        /// <summary>
        /// Extract the position/rotation of a given transform. 
        /// </summary>
        /// <param name="transform">The transform from which to extract the pose.</param>
        /// <param name="space">If the desired position/rotation is the world or local one.</param>
        /// <returns>A Pose containing the position/rotation of the transform.</returns>
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

        /// <summary>
        /// Compose two poses, applying the first to the second one.
        /// </summary>
        /// <param name="a">First pose to compose.</param>
        /// <param name="b">Pose to compose over the first one.</param>
        /// <returns>A Pose with the two operands applied.</returns>
        public static Pose Multiply(Pose a, Pose b)
        {
            var product = new Pose();
            product.position = a.position + a.rotation * b.position;
            product.rotation = a.rotation * b.rotation;
            return product;
        }

        /// <summary>
        /// Linear interpolation between two poses.
        /// </summary>
        /// <param name="a">From pose.</param>
        /// <param name="b">To pose.</param>
        /// <param name="t">Interpolation factor (0 to return a, 1 to return b).</param>
        /// <returns>A Pose between a and b</returns>
        public static Pose Lerp(Pose a, Pose b, float t)
        {
            var result = new Pose();
            result.position = Vector3.Lerp(a.position,b.position,t);
            result.rotation = Quaternion.Lerp(a.rotation, b.rotation, t);
            return result;
        }

        public static Pose Inverse(this Pose a)
        {
            var result = new Pose();
            result.position = -a.position;
            result.rotation = Quaternion.Inverse(a.rotation);
            return result;
        }

        /// <summary>
        /// Get the position/rotation difference between two transforms.
        /// </summary>
        /// <param name="to">The base transform.</param>
        /// <param name="from">The target transform.</param>
        /// <returns>A Pose indicating the position/rotation change</returns>
        public static Pose RelativeOffset(this Transform to, Transform from)
        {
            return RelativeOffset(from.position, from.rotation, to.position, to.rotation);
        }

        /// <summary>
        /// Get the position/rotation difference between a transform and a pose.
        /// </summary>
        /// <param name="to">The base transform.</param>
        /// <param name="from">The target pose.</param>
        /// <returns>A Pose indicating the offset.</returns>
        public static Pose RelativeOffset(this Transform to, Pose from)
        {
            return RelativeOffset(from.position, from.rotation, to.position, to.rotation);
        }

        /// <summary>
        /// Get the position/rotation difference between two poses.
        /// </summary>
        /// <param name="from">The base pose.</param>
        /// <param name="to">The target pose.</param>
        /// <returns>A Pose indicating the offset.</returns>
        public static Pose RelativeOffset(Pose from, Pose to)
        {
            return RelativeOffset(from.position, from.rotation, to.position, to.rotation);
        }

        /// <summary>
        /// Get the position/rotation difference between two poses, indicated with separated positions and rotations.
        /// </summary>
        /// <param name="fromPosition">The base position.</param>
        /// <param name="fromRotation">The base rotation.</param>
        /// <param name="toPosition">The target position.</param>
        /// <param name="toRotation">The target rotation.</param>
        /// <returns>A Pose indicating the offset.</returns>
        public static Pose RelativeOffset(Vector3 fromPosition, Quaternion fromRotation, Vector3 toPosition, Quaternion toRotation)
        {
            Quaternion inverseTo = Quaternion.Inverse(toRotation);
            Vector3 relativePosition = inverseTo * (fromPosition - toPosition);
            Quaternion relativeRotation = inverseTo * fromRotation;

            return new Pose(relativePosition, relativeRotation);
        }

        /// <summary>
        /// Get the world position/rotation of a relative position.
        /// </summary>
        /// <param name="reference">The transform in which the offset is local.</param>
        /// <param name="offset">The offset from the reference.</param>
        /// <returns>A Pose in world units.</returns>
        public static Pose GlobalPose(this Transform reference, Pose offset)
        {
            return new Pose(
                reference.position + reference.rotation * offset.position,
                reference.rotation * offset.rotation);
        }

        /// <summary>
        /// Indicates how similar two poses are.
        /// </summary>
        /// <param name="from">First pose to compare.</param>
        /// <param name="to">Second pose to compare.</param>
        /// <param name="maxDistance">The max distance in which the poses can be similar.</param>
        /// <returns>0 indicates no similitude, 1 for equal poses</returns>
        public static float Similitude(Pose from, Pose to, float maxDistance)
        {
            float rotationDifference = RotationDifference(from.rotation, to.rotation);
            float positionDifference = 1f - Mathf.Clamp01(Vector3.Distance(from.position, to.position) / maxDistance);
            return rotationDifference  * positionDifference;
        }

        /// <summary>
        /// Get how similar two rotations are.
        /// </summary>
        /// <param name="from">The first rotation.</param>
        /// <param name="to">The second rotation.</param>
        /// <returns>0 for opposite rotations, 1 for equal rotations.</returns>
        public static float RotationDifference(Quaternion from, Quaternion to)
        {
            float forwardDifference = Vector3.Dot(from * Vector3.forward, to * Vector3.forward) * 0.5f + 0.5f;
            float upDifference = Vector3.Dot(from * Vector3.up, to * Vector3.up) * 0.5f + 0.5f;
            return forwardDifference * upDifference;
        }

        /// <summary>
        /// Rotate a pose around an axis.
        /// </summary>
        /// <param name="pose">The pose to mirror.</param>
        /// <param name="normal">The direction of the mirror.</param>
        /// <param name="tangent">The tangent of the mirror.</param>
        /// <returns>A mirrored pose.</returns>
        public static Pose MirrorPose(this Pose pose, Vector3 normal, Vector3 tangent)
        {
            Pose mirrorPose = pose;
            Vector3 forward = pose.rotation * Vector3.forward;
            Vector3 proyectedForward = Vector3.ProjectOnPlane(forward, normal);
            float angleForward = Vector3.SignedAngle(proyectedForward, tangent, normal);
            Vector3 mirrorForward = Quaternion.AngleAxis(2 * angleForward, normal) * forward;

            Vector3 up = pose.rotation * Vector3.up;
            Vector3 proyectedUp = Vector3.ProjectOnPlane(up, normal);
            float angleUp = Vector3.SignedAngle(proyectedUp, tangent, normal);
            Vector3 mirrorUp = Quaternion.AngleAxis(2 * angleUp, normal) * up;

            mirrorPose.rotation = Quaternion.LookRotation(mirrorForward, mirrorUp);
            return mirrorPose;
        }
    }
}