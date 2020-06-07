using System.Collections.Generic;
using UnityEngine;
using static OVRSkeleton;

namespace PoseAuthoring
{
    public class HandPuppet : MonoBehaviour
    {
        [SerializeField]
        private OVRSkeleton trackedHand;

        [SerializeField]
        private List<BoneMap> boneMaps;

        private Dictionary<BoneId, BoneMap> bonesCollection;


        private void Awake()
        {
            InitializeBones();
        }


        private void InitializeBones()
        {
            bonesCollection = new Dictionary<BoneId, BoneMap>();
            foreach (var boneMap in boneMaps)
            {
                BoneId id = boneMap.id;
                bonesCollection.Add(id, boneMap);
            }
        }


        private void LateUpdate()
        {
            if (trackedHand.IsInitialized 
                && trackedHand.IsDataValid)
            {
                SetPose(trackedHand);
            }
        }


        private void SetPose(OVRSkeleton skeleton)
        {
            for (int i = 0; i < skeleton.Bones.Count; ++i)
            {
                BoneId boneId = (BoneId)skeleton.Bones[i].Id;
                if (bonesCollection.ContainsKey(boneId))
                {
                    Transform boneTransform = bonesCollection[boneId].transform;
                    Quaternion offset = Quaternion.Euler(bonesCollection[boneId].rotationOffset);
                    Quaternion desiredRot = skeleton.Bones[i].Transform.localRotation;
                    boneTransform.localRotation = offset * desiredRot;
                }
            }
        }
    }

    [System.Serializable]
    public class BoneMap
    {
        public BoneId id;
        public Transform transform;
        public Vector3 rotationOffset;
    }
}