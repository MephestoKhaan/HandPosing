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
        [SerializeField]
        private bool isRightHand;

        private Dictionary<BoneId, BoneMap> bonesCollection;
        private Dictionary<BoneId, (Vector3, Quaternion)> originalBonePosisitions;

        private bool _initialized;
        private bool _restored;

        private void Awake()
        {
            InitializeBones();
            StoreOriginalBonePositions();
            if (trackedHand == null)
            {
                this.enabled = false;
            }
        }

        private void InitializeBones()
        {
            if (_initialized)
            {
                return;
            }
            bonesCollection = new Dictionary<BoneId, BoneMap>();
            foreach (var boneMap in boneMaps)
            {
                BoneId id = boneMap.id;
                bonesCollection.Add(id, boneMap);
            }
            _initialized = true;
        }


        private void LateUpdate()
        {
            if (trackedHand != null
                && trackedHand.IsInitialized
                && trackedHand.IsDataValid)
            {
                _restored = false;
                SetLivePose(trackedHand);
            }
            else if(!_restored)
            {
                _restored = true;
                SetOriginalBonePositions();
            }
        }

        private void StoreOriginalBonePositions()
        {
            Dictionary<BoneId, (Vector3, Quaternion)> bonePositions = new Dictionary<BoneId, (Vector3, Quaternion)>();
            foreach (var boneMap in boneMaps)
            {
                Vector3 localPosition = boneMap.transform.localPosition;
                Quaternion localRotation = boneMap.transform.localRotation;
                bonePositions.Add(boneMap.id, (localPosition, localRotation));
            }
            originalBonePosisitions = bonePositions;
        }

        private void SetOriginalBonePositions()
        {
            foreach (var bonePosition in originalBonePosisitions)
            {
                Transform bone = bonesCollection[bonePosition.Key].transform;
                bone.localPosition = bonePosition.Value.Item1;
                bone.localRotation = bonePosition.Value.Item2;
            }
        }


        private void SetLivePose(OVRSkeleton skeleton)
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

                    if (bonesCollection[boneId].updatePosition)
                    {
                        boneTransform.position = skeleton.Bones[i].Transform.position;
                    }
                }
            }
        }

        public void SetRecordedPose(HandPose pose, Transform relativeTo)
        {
            InitializeBones();
            foreach (var bone in pose.Bones)
            {
                BoneId boneId = bone.boneID;
                if (bonesCollection.ContainsKey(boneId))
                {
                    Transform boneTransform = bonesCollection[boneId].transform;
                    boneTransform.localRotation = bone.rotation;
                }
            }
            if (relativeTo != null)
            {
                this.transform.localPosition = pose.handPosition;
                this.transform.localRotation = pose.handRotation;
            }
            else
            {
                this.transform.position = pose.handPosition;
                this.transform.rotation = pose.handRotation;
            }
        }

        public HandPose CurrentPose(Transform respect)
        {
            HandPose pose = new HandPose();
            foreach (var bone in bonesCollection)
            {
                BoneMap boneMap = bone.Value;
                Quaternion rotation = boneMap.transform.localRotation;
                pose.Bones.Add(new BoneRotation() { boneID = boneMap.id, rotation = rotation });
            }
            pose.handPosition = respect != null ? respect.InverseTransformPoint(this.transform.position) : this.transform.position;
            pose.handRotation = respect != null ? respect.rotation * this.transform.rotation : this.transform.rotation;
            pose.isRightHand = isRightHand;
            return pose;
        }
    }

}