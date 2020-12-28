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
	/// Data for a bone of hand.
	/// </summary>
	public struct HandBone 
    {
		/// <summary>
		/// The unique identifier of the bone.
		/// </summary>
		public BoneId Id { get; private set; }
		/// <summary>
		/// The transform this bone drives.
		/// </summary>
		public Transform Transform { get; private set; }

		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <param name="id">Unique identifier of the bone.</param>
		/// <param name="transform">Transform that this bone drives.</param>
		public HandBone(BoneId id, Transform transform)
        {
			Id = id;
			Transform = transform;
        }
	}
}