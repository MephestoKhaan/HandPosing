using System.Collections.Generic;
using UnityEngine;

namespace HandPosing
{
    /// <summary>
    /// Stores the translation between hand tracked data and the represented bone.
    /// </summary>
    [System.Serializable]
    public class BoneMap
    {
        /// <summary>
        /// The unique identifier for the bone.
        /// </summary>
        public BoneId id;
        /// <summary>
        /// The trasform that this bone drives.
        /// </summary>
        public Transform transform;
        /// <summary>
        /// The rotation difference between the hand-tracked bone, and the represented bone.
        /// </summary>
        public Vector3 rotationOffset;

        /// <summary>
        /// Get the rotationOffset as a Quaternion.
        /// </summary>
        public Quaternion RotationOffset
        {
            get
            {
                return Quaternion.Euler(rotationOffset);
            }
        }

        /// <summary>
        /// Get the raw rotation of the bone, as taken from the tracking data
        /// </summary>
        public Quaternion TrackedRotation
        {
            get
            {
                return Quaternion.Inverse(RotationOffset) * transform.localRotation;
            }
        }
    }

    /// <summary>
    /// A special mapping for the base of the hand, indicating the position and rotation difference
    /// between the hand-tracking system and its representation.
    /// </summary>
    [System.Serializable]
    public class HandMap
    {
        /// <summary>
        /// The unique identifier for the base of the hand. Tipically the wrist.
        /// </summary>
        public BoneId id;
        /// <summary>
        /// The rotation difference at the base of the hand between the hand-tracking system and the representation.
        /// </summary>
        public Vector3 rotationOffset;
        /// <summary>        
        /// The position difference at the base of the hand between the hand-tracking system and the representation.
        /// </summary>
        public Vector3 positionOffset;

        /// <summary>
        /// Get the rotationOffset as a quaternion.
        /// </summary>
        public Quaternion RotationOffset
        {
            get
            {
                return Quaternion.Euler(rotationOffset);
            }
        }

        /// <summary>
        /// Get the position/rotation offset as a more compact Pose.
        /// </summary>
        public Pose Offset
        {
            get
            {
                return new Pose(positionOffset, RotationOffset);
            }
        }

        /// <summary>
        /// Set the position/rotation of the hand to the default value.
        /// </summary>
        public void Apply(Transform t)
        {
            t.localPosition = this.positionOffset;
            t.localRotation = RotationOffset;
        }
    }

    /// <summary>
    /// A collection of bone maps to transform between hand-tracking data and their representation.
    /// Implements ISerializationCallbackReceiver to be able to store data as a Dictionary that survives serialization callbacks.
    /// </summary>
    [System.Serializable]
    public class BoneCollection : Dictionary<BoneId, BoneMap>, ISerializationCallbackReceiver
    {
        [SerializeField]
        [HideInInspector]
        private List<BoneId> serialisedKeys = new List<BoneId>();
        [SerializeField]
        [HideInInspector]
        private List<BoneMap> serialisedValues = new List<BoneMap>();

        public void OnAfterDeserialize()
        {
            if(serialisedKeys != null)
            {
                this.Clear();
                for(int i = 0; i < serialisedKeys.Count; i++)
                {
                    this.Add(serialisedKeys[i], serialisedValues[i]);
                }
            }
        }

        public void OnBeforeSerialize()
        {
            serialisedValues.Clear();
            serialisedKeys.Clear();
            foreach (var item in this)
            {
                serialisedKeys.Add(item.Key);
                serialisedValues.Add(item.Value);
            }
        }
    };
}