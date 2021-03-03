using System.Collections.Generic;
using UnityEngine;

namespace HandPosing
{
    /// <summary>
    /// Handeness of a hand.
    /// </summary>
    public enum Handeness
    {
        Left, Right
    }

    /// <summary>
    /// Data for the pose of a hand in space.
    /// </summary>
    [System.Serializable]
    public struct HandPose
    {
        /// <summary>
        /// Relative position/rotation of the grip point of the hand.
        /// </summary>
        public Pose relativeGrip;
        /// <summary>
        /// Handeness of the hand.
        /// </summary>
        public Handeness handeness;

        [SerializeField]
        private List<BonePose> _bones;
        /// <summary>
        /// Collection of bones and their rotations in this hand.
        /// </summary>
        public List<BonePose> Bones
        {
            get
            {
                if (_bones == null)
                {
                    _bones = new List<BonePose>();
                }
                return _bones;
            }
        }

        /// <summary>
        /// Gets the offset of the hand in relation to another transform.
        /// </summary>
        /// <param name="relativeTo">The transform from which to measure the offset.</param>
        /// <returns>A Pose indicating the offset.</returns>
        public Pose ToPose(Transform relativeTo)
        {
            Vector3 globalPosDesired = relativeTo.TransformPoint(relativeGrip.position);
            Quaternion globalRotDesired = relativeTo.rotation * relativeGrip.rotation;
            return new Pose(globalPosDesired, globalRotDesired);
        }

        /// <summary>
        /// Moves the hand so the grip alligns with a point in space.
        /// Note that this method moves the entire hand but does not modify the fingers.
        /// </summary>
        /// <param name="pose">The offset from the anchor transform at which to allign the grip of the hand.</param>
        /// <param name="relativeTo">The anchor transform from which to measure the offset pose.</param>
        /// <returns>A copy of the HandPose propery alligned.</returns>
        public HandPose AdjustPose(Pose pose, Transform relativeTo)
        {
            HandPose snapPose = this;
            snapPose.relativeGrip.position = relativeTo.InverseTransformPoint(pose.position);
            snapPose.relativeGrip.rotation = Quaternion.Inverse(relativeTo.rotation) * pose.rotation;
            return snapPose;
        }

        /// <summary>
        /// Interpolates between two HandPoses, if they have the same handeness and bones.
        /// </summary>
        /// <param name="from">Base HandPose to interpolate from.</param>
        /// <param name="to">Target HandPose to interpolate to.</param>
        /// <param name="t">Interpolation factor, 0 for the base, 1 for the target.</param>
        /// <returns>A new HandPose positioned/rotated between the base and target, null if the hands cannot be interpolated.</returns>
        public static HandPose? Lerp(HandPose from, HandPose to, float t)
        {
            if(from.handeness != to.handeness)
            {
                Debug.LogError("Hand poses must have same handenness for lerping");
                return null;
            }
            if(from.Bones.Count != to.Bones.Count)
            {
                Debug.LogError("Hand poses must have same Bones for lerping");
                return null;
            }

            HandPose result = new HandPose();
            result.relativeGrip = PoseUtils.Lerp(from.relativeGrip, to.relativeGrip, t);
            result.handeness = from.handeness;

            result.Bones.Clear();
            for (int i = 0; i < from.Bones.Count; i++)
            {
                BonePose? bone = BonePose.Lerp(from.Bones[i], to.Bones[i], t);
                if(bone.HasValue)
                {
                    result.Bones.Add(bone.Value);
                }
                else
                {
                    Debug.LogError("Hand poses must have same Bones for lerping");
                    return null;
                }
            }

            return result;

        }
    }

}