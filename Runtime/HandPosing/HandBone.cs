using UnityEngine;

namespace HandPosing
{
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

	public struct HandBone 
    {
		public BoneId Id { get; private set; }
		public Transform Transform { get; private set; }

		public HandBone(BoneId id, Transform transform)
        {
			Id = id;
			Transform = transform;
        }
	}
}