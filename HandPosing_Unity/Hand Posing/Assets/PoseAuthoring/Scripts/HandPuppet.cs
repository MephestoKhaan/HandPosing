using System.Collections.Generic;
using UnityEngine;
using static OVRSkeleton;
using static PoseAuthoring.HandSnapPose;

namespace PoseAuthoring
{
    [DefaultExecutionOrder(-10)]
    public class HandPuppet : MonoBehaviour
    {
        [SerializeField]
        private OVRSkeleton trackedHand;
        [SerializeField]
        private Animator animator;
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

        public System.Action OnPoseWillUpdate;
        public System.Action OnPoseUpdated;

        private class BoneCollection : Dictionary<BoneId, BoneMap> { };
        private BoneCollection _bonesCollection;

        private HandMap _originalHandOffset;
        private Pose _originalGripOffset;
        private Pose _pupettedGripOffset;

        private Pose TrackedGripPose
        {
            get
            {
                Pose offset = _trackingHands ? _pupettedGripOffset : _originalGripOffset;
                return this.handAnchor.GlobalPose(offset);
            }
        }


        private bool _trackingHands;
        private bool _usingOVRUpdates;

        private void Awake()
        {
            _bonesCollection = InitializeBones();

            if (trackedHand == null)
            {
                this.enabled = false;
            }

            InitializeOVRUpdates();
        }

        private BoneCollection InitializeBones()
        {
            var bonesCollection = new BoneCollection();
            foreach (var boneMap in boneMaps)
            {
                BoneId id = boneMap.id;
                bonesCollection.Add(id, boneMap);
            }

            return bonesCollection;
        }

        private void InitializeOVRUpdates()
        {
            OVRCameraRig rig = this.transform.GetComponentInParent<OVRCameraRig>();
            if (rig != null)
            {
                rig.UpdatedAnchors += (r) => { UpdateHandPose(); };
                _usingOVRUpdates = true;
            }
            else
            {
                _usingOVRUpdates = false;
            }
        }



        private void Start()
        {
            StorePoses();
        }

        private void StorePoses()
        {
            _originalHandOffset = HandOffsetMapping();
            _originalGripOffset = GripOffset;
            _pupettedGripOffset = OffsetedGripPose(trackedHandOffset.positionOffset,
                trackedHandOffset.RotationOffset * Quaternion.Euler(0f, 180f, 0f));
        }

        private Pose OffsetedGripPose(Vector3 posOffset, Quaternion rotOffset)
        {
            Transform hand = trackedHandOffset.transform;
            Vector3 originalPos = hand.localPosition;
            Quaternion originalRot = hand.localRotation;
            hand.localRotation = rotOffset;
            hand.localPosition = hand.localPosition + posOffset;

            Pose pose = GripOffset;

            hand.localRotation = originalRot;
            hand.localPosition = originalPos;

            return pose;
        }


        private void Update()
        {
            OnPoseWillUpdate?.Invoke();
            if (!_usingOVRUpdates)
            {
                UpdateHandPose();
            }
        }

        private void UpdateHandPose()
        {
            if (trackedHand != null
                && trackedHand.IsInitialized
                && trackedHand.IsDataValid)
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
                animator.enabled = false;
            }
            SetLivePose(trackedHand);
        }

        private void DisableHandTracked()
        {
            if (_trackingHands)
            {
                _trackingHands = false;
                animator.enabled = true;
                RestoreHandOffset();
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

        public void RestoreHandOffset()
        {
            _originalHandOffset.transform.localPosition = _originalHandOffset.positionOffset;
            _originalHandOffset.transform.localRotation = _originalHandOffset.RotationOffset;
        }
        #endregion

        private void SetLivePose(OVRSkeleton skeleton)
        {
            for (int i = 0; i < skeleton.Bones.Count; ++i)
            {
                BoneId boneId = skeleton.Bones[i].Id;
                if (_bonesCollection.ContainsKey(boneId))
                {
                    Transform boneTransform = _bonesCollection[boneId].transform;
                    boneTransform.localRotation = UnmapRotation(skeleton.Bones[i],
                        _bonesCollection[boneId].RotationOffset);
                }
                else if (trackedHandOffset.id == boneId) //TODO, do I REALLY want to move this?
                {
                    Transform boneTransform = trackedHandOffset.transform;
                    boneTransform.localRotation = UnmapRotation(skeleton.Bones[i],
                     trackedHandOffset.RotationOffset);

                    boneTransform.localPosition = trackedHandOffset.positionOffset
                        + skeleton.Bones[i].Transform.localPosition;
                }
            }

            Quaternion UnmapRotation(OVRBone trackedBone, Quaternion rotationOffset)
            {
                return rotationOffset * trackedBone.Transform.localRotation;
            }
        }

        #region pose lerping

        public void LerpToPose(HandSnapPose pose, Transform relativeTo, float bonesWeight = 1f, float positionWeight = 1f)
        {
            LerpBones(pose, bonesWeight);
            LerpGripOffset(pose, positionWeight, relativeTo);
        }

        public void LerpBones(HandSnapPose pose, float weight)
        {
            if (weight > 0f)
            {
                foreach (var bone in pose.Bones)
                {
                    BoneId boneId = bone.boneID;
                    if (_bonesCollection.ContainsKey(boneId))
                    {
                        Transform boneTransform = _bonesCollection[boneId].transform;
                        boneTransform.localRotation = Quaternion.Lerp(boneTransform.localRotation, bone.rotation, weight);
                    }
                }
            }
        }



        public void LerpGripOffset(HandSnapPose pose, float weight, Transform relativeTo)
        {
            Pose offset = new Pose(pose.relativeGripPos, pose.relativeGripRot);
            LerpGripOffset(offset, weight, relativeTo);
        }

        public void LerpGripOffset(Pose pose, float weight, Transform relativeTo = null)
        {
            relativeTo = relativeTo ?? this.handAnchor;

            Pose worldGrip = TrackedGripPose;

            Quaternion rotationDif = Quaternion.Inverse(transform.rotation) * this.gripPoint.rotation;
            Quaternion desiredRotation = (relativeTo.rotation * pose.rotation) * rotationDif;
            Quaternion trackedRot = rotationDif * worldGrip.rotation;
            Quaternion finalRot = Quaternion.Lerp(trackedRot, desiredRotation, weight);
            transform.rotation = finalRot;

            Vector3 positionDif = transform.position - this.gripPoint.position;
            Vector3 desiredPosition = relativeTo.TransformPoint(pose.position) + positionDif;
            Vector3 trackedPosition = worldGrip.position + positionDif;
            Vector3 finalPos = Vector3.Lerp(trackedPosition, desiredPosition, weight);
            transform.position = finalPos;
        }

        #endregion

        #region currentPoses

        public HandSnapPose TrackedPose(Transform relativeTo, bool includeBones = false)
        {
            Pose worldGrip = TrackedGripPose;
            Vector3 trackedGripPosition = worldGrip.position;
            Quaternion trackedGripRotation = worldGrip.rotation;

            HandSnapPose pose = new HandSnapPose();
            pose.relativeGripPos = relativeTo.InverseTransformPoint(trackedGripPosition);
            pose.relativeGripRot = Quaternion.Inverse(relativeTo.rotation) * trackedGripRotation;
            pose.handeness = this.handeness;

            if(includeBones)
            {
                foreach (var bone in _bonesCollection)
                {
                    BoneMap boneMap = bone.Value;
                    Quaternion rotation = boneMap.transform.localRotation;
                    pose.Bones.Add(new BoneRotation() { boneID = boneMap.id, rotation = rotation });
                }
            }

            return pose;
        }

        public HandSnapPose VisualPose(Transform relativeTo)
        {
            HandSnapPose pose = new HandSnapPose();
            pose.relativeGripPos = relativeTo.InverseTransformPoint(this.gripPoint.position);
            pose.relativeGripRot = Quaternion.Inverse(relativeTo.rotation) * this.gripPoint.rotation;
            pose.handeness = this.handeness;

            foreach (var bone in _bonesCollection)
            {
                BoneMap boneMap = bone.Value;
                Quaternion rotation = boneMap.transform.localRotation;
                pose.Bones.Add(new BoneRotation() { boneID = boneMap.id, rotation = rotation });
            }
            return pose;
        }
        #endregion
    }
}