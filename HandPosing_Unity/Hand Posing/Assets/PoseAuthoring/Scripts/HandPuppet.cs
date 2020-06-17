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

        public Dictionary<BoneId, BoneMap> BonesCollection { get; private set; }

        private bool _initialized;

        private void Awake()
        {
            InitializeBones();

            if(trackedHand == null)
            {
                this.enabled = false;
            }
        }

        private void InitializeBones()
        {
            if(_initialized)
            {
                return;
            }
            BonesCollection = new Dictionary<BoneId, BoneMap>();
            foreach (var boneMap in boneMaps)
            {
                BoneId id = boneMap.id;
                BonesCollection.Add(id, boneMap);
            }
            _initialized = true;
        }


        private void LateUpdate()
        {
            if (trackedHand != null
                && trackedHand.IsInitialized 
                && trackedHand.IsDataValid)
            {
                SetLivePose(trackedHand);
            }
        }


        private void SetLivePose(OVRSkeleton skeleton)
        {
            for (int i = 0; i < skeleton.Bones.Count; ++i)
            {
                BoneId boneId = (BoneId)skeleton.Bones[i].Id;
                if (BonesCollection.ContainsKey(boneId))
                {
                    Transform boneTransform = BonesCollection[boneId].transform;
                    Quaternion offset = Quaternion.Euler(BonesCollection[boneId].rotationOffset);
                    Quaternion desiredRot = skeleton.Bones[i].Transform.localRotation;
                    boneTransform.localRotation = offset * desiredRot;
                }
            }
        }

        public void SetRecordedPose(HandPose pose, Transform relativeTo)
        {
            InitializeBones();
            foreach (var bone in pose.Bones)
            {
                BoneId boneId = bone.boneID;
                if (BonesCollection.ContainsKey(boneId))
                {
                    Transform boneTransform = BonesCollection[boneId].transform;
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
            foreach (var bone in BonesCollection)
            {
                BoneMap boneMap = bone.Value;
                Quaternion rotation = boneMap.transform.localRotation;
                pose.Bones.Add(new BoneRotation() { boneID = boneMap.id, rotation = rotation });
            }
            pose.handPosition = respect != null? respect.InverseTransformPoint(this.transform.position) : this.transform.position;
            pose.handRotation = respect != null ? respect.rotation *  this.transform.rotation : this.transform.rotation;
            pose.isRightHand = isRightHand;
            return pose;
        }
    }

}