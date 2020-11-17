using System.Collections.Generic;
using UnityEngine;

namespace PoseAuthoring
{
    [System.Serializable]
    public class BoneMap
    {
        public BoneId id;
        public Transform transform;
        public Vector3 rotationOffset;

        public Quaternion RotationOffset
        {
            get
            {
                return Quaternion.Euler(rotationOffset);
            }
        }
    }

    [System.Serializable]
    public class HandMap
    {
        public BoneId id;
        public Transform transform;
        public Vector3 rotationOffset;
        public Vector3 positionOffset;

        public Quaternion RotationOffset
        {
            get
            {
                return Quaternion.Euler(rotationOffset);
            }
        }
    }

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