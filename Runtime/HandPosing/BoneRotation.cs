using UnityEngine;

namespace HandPosing
{
    /// <summary>
    /// Valid identifiers for the bones of a hand.
    /// </summary>
    public enum BoneId
    {
        Invalid,
        Hand_Start,
        Hand_Thumb0,
        Hand_Thumb1,
        Hand_Thumb2,
        Hand_Thumb3,
        Hand_Index1,
        Hand_Index2,
        Hand_Index3,
        Hand_Middle1,
        Hand_Middle2,
        Hand_Middle3,
        Hand_Ring1,
        Hand_Ring2,
        Hand_Ring3,
        Hand_Pinky0,
        Hand_Pinky1,
        Hand_Pinky2,
        Hand_Pinky3
    }
    /// <summary>
    /// Data indicating the rotation of a bone from a hand.
    /// </summary>
    [System.Serializable]
    public struct BonePose
    {
        /// <summary>
        /// The unique identifier of the bone.
        /// </summary>
        public BoneId boneID;
        /// <summary>
        /// The rotation of the bone in the hand.
        /// </summary>
        public Quaternion rotation;
        /// <summary>
        /// The position of the bone in the hand.
        /// </summary>
        public Vector3 position;

        /// <summary>
        /// Gets the interpolated value between two rotations of the same bone (same indentifier).
        /// </summary>
        /// <param name="from">Base rotation to interpolate from.</param>
        /// <param name="to">Target rotation to interpolate to.</param>
        /// <param name="t">Interpolation factor, 0 for base, 1 for target rotation</param>
        /// <returns>A new BonePose between base and target, null if bone is invalid.</returns>
        public static BonePose? Lerp(BonePose from, BonePose to, float t)
        {
            if (from.boneID != to.boneID)
            {
                Debug.LogError("Bones must have same id for interpolation");
                return null;
            }

            return new BonePose()
            {
                boneID = from.boneID,
                rotation = Quaternion.Lerp(from.rotation, to.rotation, t),
                position = Vector3.Lerp(from.position, to.position, t)
            };

        }
    }
}