using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HandPosing
{
    [DefaultExecutionOrder(-10)]
    public class HandPuppet : MonoBehaviour
    {
        [SerializeField]
        private SkeletonDataProvider skeleton;
        [SerializeField]
        private AnchorsUpdateNotifier updateNotifier;
        [SerializeField]
        private Transform handAnchor;
        [SerializeField]
        private Transform gripPoint;
        [SerializeField]
        private Handeness handeness;

        [SerializeField]
        private HandMap trackedHandOffset;
        [SerializeField]
        private List<BoneMap> boneMaps;

        [SerializeField]
        private UnityEvent OnUsingHands;
        [SerializeField]
        private UnityEvent OnUsingControllers;

        public List<BoneMap> Bones
        {
            get
            {
                return boneMaps;
            }
        }

        public HandMap HandOffset
        {
            get
            {
                return trackedHandOffset;
            }
        }

        public float Scale
        {
            get
            {
                return this.handAnchor.localScale.x;
            }
            private set
            {
                this.handAnchor.localScale = Vector3.one * value;
            }
        }

        public Transform Grip
        {
            get
            {
                return gripPoint;
            }
        }
        public bool IsTrackingHands
        {
            get
            {
                return _trackingHands;
            }
        }

        public Pose GripOffset
        {
            get
            {
                return this.handAnchor.RelativeOffset(this.gripPoint);
            }
        }

        public System.Action OnPoseBeforeUpdate;
        public System.Action OnPoseUpdated;

        private BoneCollection _bonesCache;
        private BoneCollection BonesCache
        {
            get
            {
                if (_bonesCache == null)
                {
                    _bonesCache = CacheBones();
                }
                return _bonesCache;
            }
        }

        private HandMap _originalHandOffset;
        private Pose _originalGripOffset;
        private Pose _pupettedGripOffset;
        private bool _offsetInitialised = false;
        private bool _usingUpdateNotifier;

        public Pose TrackedGripPose
        {
            get
            {
                if (!_offsetInitialised)
                {
                    CacheGripOffsets();
                }
                Pose offset = _trackingHands ? _pupettedGripOffset : _originalGripOffset;
                offset.position = offset.position * Scale;
                return this.handAnchor.GlobalPose(offset);
            }
        }

        private bool _trackingHands;

        private void Awake()
        {
            if (skeleton == null)
            {
                this.enabled = false;
            }
            else
            {
                if (updateNotifier != null)
                {
                    updateNotifier.OnAnchorsEveryUpdate += UpdateHandPose;
                    _usingUpdateNotifier = true;
                }
                else
                {
                    _usingUpdateNotifier = false;
                }
            }
            CacheGripOffsets();
        }

        private BoneCollection CacheBones()
        {
            var bonesCollection = new BoneCollection();
            foreach (var boneMap in boneMaps)
            {
                BoneId id = boneMap.id;
                bonesCollection.Add(id, boneMap);
            }
            return bonesCollection;
        }


        private void CacheGripOffsets()
        {
            _originalHandOffset = HandOffsetMapping();
            _originalGripOffset = this.handAnchor.RelativeOffset(this.gripPoint);
            _pupettedGripOffset = OffsetedGripPose();
            _offsetInitialised = true;
        }

        private Pose OffsetedGripPose()
        {
            Pose trackingOffset = new Pose(Vector3.zero, Quaternion.Euler(0f, 180f, 0f));
            Pose gripOffset = this.transform.RelativeOffset(this.gripPoint);
            Pose hand = PoseUtils.Multiply(trackedHandOffset.Offset, trackingOffset);
            Pose translateGrip = PoseUtils.Multiply(hand, gripOffset);
            return translateGrip;
        }

        public Transform test;

        private void Update()
        {
            OnPoseBeforeUpdate?.Invoke();
            if (!_usingUpdateNotifier)
            {
                UpdateHandPose();
            }

            if(test != null)
            {
                test.position = TrackedGripPose.position;
            }
        }

        private void UpdateHandPose()
        {
            if (skeleton != null
                && skeleton.IsTracking)
            {
                EnableHandTracked();
            }
            else
            {
                DisableHandTracked();
            }
            OnPoseUpdated?.Invoke();
        }

        private void EnableHandTracked()
        {
            if (!_trackingHands)
            {
                _trackingHands = true;
                Scale = skeleton.HandScale ?? 1f;
                OnUsingHands?.Invoke();
            }
            SetLivePose(skeleton.Bones);
        }

        private void DisableHandTracked()
        {
            if (_trackingHands)
            {
                _trackingHands = false;
                Scale = 1f;
                OnUsingControllers?.Invoke();
                _originalHandOffset.Apply();
            }
        }

        #region bone restoring
        private HandMap HandOffsetMapping()
        {
            return new HandMap()
            {
                id = trackedHandOffset.id,
                transform = trackedHandOffset.transform,
                positionOffset = trackedHandOffset.transform.localPosition,
                rotationOffset = trackedHandOffset.transform.localRotation.eulerAngles
            };
        }
        #endregion

        private void SetLivePose(List<HandBone> Bones)
        {
            for (int i = 0; i < Bones.Count; ++i)
            {
                BoneId boneId = Bones[i].Id;
                if (BonesCache.ContainsKey(boneId))
                {
                    Transform boneTransform = BonesCache[boneId].transform;
                    boneTransform.localRotation = BonesCache[boneId].RotationOffset
                        * Bones[i].Transform.localRotation;
                }
                else if (trackedHandOffset.id == boneId)
                {
                    Transform handTransform = trackedHandOffset.transform;
                    handTransform.localPosition = trackedHandOffset.positionOffset
                         + trackedHandOffset.RotationOffset * Bones[i].Transform.localPosition;
                    handTransform.localRotation = trackedHandOffset.RotationOffset
                         * Bones[i].Transform.localRotation;
                }
            }
        }

        #region pose lerping

        public void LerpToPose(HandPose pose, Transform relativeTo, float bonesWeight = 1f, float positionWeight = 1f)
        {
            LerpBones(pose.Bones, bonesWeight);
            LerpGripOffset(pose, positionWeight, relativeTo);
        }

        public void LerpBones(List<BoneRotation> bones, float weight)
        {
            if (weight > 0f)
            {
                foreach (var bone in bones)
                {
                    BoneId boneId = bone.boneID;
                    if (BonesCache.ContainsKey(boneId))
                    {
                        Transform boneTransform = BonesCache[boneId].transform;
                        boneTransform.localRotation = Quaternion.Lerp(boneTransform.localRotation, bone.rotation, weight);
                    }
                }
            }
        }

        public void LerpGripOffset(HandPose pose, float weight, Transform relativeTo)
        {
            LerpGripOffset(pose.relativeGrip, weight, relativeTo);
        }

        public void LerpGripOffset(Pose pose, float weight, Transform relativeTo = null)
        {
            Pose gripOffset = this.gripPoint.RelativeOffset(this.transform);
            Pose basic = TrackedGripPose;
            Pose target = (relativeTo ?? this.handAnchor).GlobalPose(pose);
            Pose result = PoseUtils.Lerp(basic, target, weight);
            result = PoseUtils.Multiply(result, gripOffset);
            this.transform.SetPose(result);
        }
        #endregion

        #region currentPoses

        public HandPose TrackedPose(Transform relativeTo, bool includeBones = false)
        {
            HandPose pose = new HandPose();

            pose.relativeGrip = relativeTo.RelativeOffset(TrackedGripPose);
            pose.handeness = this.handeness;

            if (includeBones)
            {
                foreach (var bone in BonesCache)
                {
                    BoneMap boneMap = bone.Value;
                    Quaternion rotation = boneMap.transform.localRotation;
                    pose.Bones.Add(new BoneRotation() { boneID = boneMap.id, rotation = rotation });
                }
            }
            return pose;
        }
        #endregion
    }
}