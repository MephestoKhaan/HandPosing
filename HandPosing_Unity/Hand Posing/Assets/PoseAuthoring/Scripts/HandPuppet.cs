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

        public Transform Anchor
        {
            get
            {
                return handAnchor;
            }
        }

        private Dictionary<BoneId, BoneMap> _bonesCollection;
        private HandMap _controlledHandOffset;

        public System.Action OnPuppetUpdated;

        private Pose _originalGripOffset;
        private Pose _pupettedGripOffset;
        private Pose WorldGripPose
        {
            get
            {
                Pose offset = _puppettedHand ? _pupettedGripOffset : _originalGripOffset;
                return this.handAnchor.GlobalPose(offset);
            }
        }

        private bool _initialized;
        private bool _restored;
        private bool _puppettedHand;

        private bool _operatingWithoutOVRCameraRig = true;
        private bool alreadyUpdated;

        private void Awake()
        {
            InitializeBones();

            if (trackedHand == null)
            {
                this.enabled = false;
            }


            OVRCameraRig rig = transform.GetComponentInParent<OVRCameraRig>();
            if (rig != null)
            {
                rig.UpdatedAnchors += (r) => { OnUpdatedAnchors(); };
                _operatingWithoutOVRCameraRig = false;
            }
        }

        private void Start()
        {
            StoreOriginalBonePositions();

            _originalGripOffset = this.handAnchor.RelativeOffset(this.gripPoint);
            _pupettedGripOffset = OffsetedGripPose();

        }

        private Pose OffsetedGripPose()
        {
            Transform hand = trackedHandOffset.transform;
            Vector3 p = hand.localPosition;
            Quaternion r = hand.localRotation;

            hand.localRotation = trackedHandOffset.RotationOffset * Quaternion.Euler(0f,180f,0f);
            hand.localPosition = hand.localPosition + trackedHandOffset.positionOffset;

            Pose pose = this.handAnchor.RelativeOffset(this.gripPoint);
            hand.localRotation = r;
            hand.localPosition = p;

            return pose;
        }

        private void InitializeBones()
        {
            if (_initialized)
            {
                return;
            }
            _bonesCollection = new Dictionary<BoneId, BoneMap>();
            foreach (var boneMap in boneMaps)
            {
                BoneId id = boneMap.id;
                _bonesCollection.Add(id, boneMap);
            }
            _initialized = true;
        }

        private void Update()
        {
            alreadyUpdated = false;
            if (_operatingWithoutOVRCameraRig)
            {
                OnUpdatedAnchors();
            }
        }


        private void OnUpdatedAnchors()
        {
            if (alreadyUpdated) return;
            alreadyUpdated = true;

            if (trackedHand != null
                && trackedHand.IsInitialized
                && trackedHand.IsDataValid)
            {
                _restored = false;
                EnableHandTracked();

            }
            else if (!_restored)
            {
                _restored = true;
                DisableHandTracked();
            }

            OnPuppetUpdated?.Invoke();
        }

        private void EnableHandTracked()
        {
            SetLivePose(trackedHand);
            if (!_puppettedHand)
            {
                animator.enabled = false;
            }
            _puppettedHand = true;
        }

        private void DisableHandTracked()
        {
            if (_puppettedHand)
            {
                animator.enabled = true;
            }
            _puppettedHand = false;
            SetOriginalBonePositions();

        }

        #region bone restoring
        private void StoreOriginalBonePositions()
        {
            _controlledHandOffset = new HandMap()
            {
                id = trackedHandOffset.id,
                transform = trackedHandOffset.transform,
                positionOffset = trackedHandOffset.transform.localPosition,
                rotationOffset = trackedHandOffset.transform.localRotation.eulerAngles
            };
        }

        public void SetOriginalBonePositions()
        {
            _controlledHandOffset.transform.localPosition = _controlledHandOffset.positionOffset;
            _controlledHandOffset.transform.localRotation = Quaternion.Euler(_controlledHandOffset.rotationOffset);
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
                else if (trackedHandOffset.id == boneId)
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

        public void SetDefaultPose()
        {
            Pose worldGrip = WorldGripPose;
            Quaternion rotationDif = Quaternion.Inverse(this.transform.rotation) * this.gripPoint.rotation;
            Quaternion trackedRot = rotationDif * worldGrip.rotation;

            Vector3 positionDif = this.transform.position - this.gripPoint.position;
            Vector3 trackedPosition = worldGrip.position + positionDif;

            this.transform.rotation = trackedRot;
            this.transform.position = trackedPosition;
        }

        public void LerpToPose(HandSnapPose pose, Transform relativeTo, float bonesWeight = 1f, float positionWeight = 1f)
        {
            LerpBones(pose, bonesWeight);
            LerpOffset(pose, relativeTo, positionWeight);
        }

        public void LerpBones(HandSnapPose pose, float weight)
        {
            InitializeBones();

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

        public void LerpOffset(HandSnapPose pose, Transform relativeTo, float weight)
        {
            Pose worldGrip = WorldGripPose;

            Quaternion rotationDif = Quaternion.Inverse(this.transform.rotation) * this.gripPoint.rotation;
            Quaternion desiredRotation = (relativeTo.rotation * pose.relativeGripRot) * rotationDif;
            Quaternion trackedRot = rotationDif * worldGrip.rotation;
            Quaternion finalRot = Quaternion.Lerp(trackedRot, desiredRotation, weight);
            this.transform.rotation = finalRot;

            Vector3 positionDif = this.transform.position - this.gripPoint.position;
            Vector3 desiredPosition = relativeTo.TransformPoint(pose.relativeGripPos) + positionDif;
            Vector3 trackedPosition = worldGrip.position + positionDif;
            Vector3 finalPos = Vector3.Lerp(trackedPosition, desiredPosition, weight);
            this.transform.position = finalPos;
        }


        public HandSnapPose CurrentPoseVisual(Transform relativeTo)
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

        public HandSnapPose CurrentPoseTracked(Transform relativeTo)
        {
            var gripPose = WorldGripPose;
            Vector3 trackedGripPosition = gripPose.position;
            Quaternion trackedGripRotation = gripPose.rotation;

            HandSnapPose pose = new HandSnapPose();
            pose.relativeGripPos = relativeTo.InverseTransformPoint(trackedGripPosition);
            pose.relativeGripRot = Quaternion.Inverse(relativeTo.rotation) * trackedGripRotation;
            pose.handeness = this.handeness;
            return pose;
        }
    }

}