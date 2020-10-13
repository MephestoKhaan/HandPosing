using System.Collections.Generic;
using UnityEngine;
using static OVRSkeleton;
using static PoseAuthoring.HandSnapPose;

namespace PoseAuthoring
{
    [DefaultExecutionOrder(-50)]
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

        private Dictionary<BoneId, BoneMap> _bonesCollection;
        private HandMap _controlledHandOffset;

        public System.Action OnPuppetUpdated;

        public Pose TrackedPose
        {
            get;
            private set;
        }

        private Pose _originalGripOffset;
        private Pose? _pupettedGripOffset;
        private Pose WorldGripPose
        {
            get
            {
                Pose offset = _puppettedHand ? _pupettedGripOffset.Value : _originalGripOffset;
                Vector3 trackedGripPosition = this.handAnchor.TransformPoint(offset.position);
                Quaternion trackedGripRotation = this.handAnchor.rotation * offset.rotation;
                return new Pose(trackedGripPosition, trackedGripRotation);
            }
        }

        private bool _initialized;
        private bool _restored;
        private bool _puppettedHand;

        private void Awake()
        {
            InitializeBones();

            if (trackedHand == null)
            {
                this.enabled = false;
            }
        }

        private void Start()
        {
            StoreOriginalBonePositions();
            _originalGripOffset = CalculateGripOffset();
        }

        private Pose CalculateGripOffset()
        {
            Vector3 relativePosition = this.handAnchor.InverseTransformPoint(this.gripPoint.position);
            Quaternion relativeRotation = Quaternion.Inverse(this.handAnchor.rotation) * this.gripPoint.rotation;
            return new Pose(relativePosition, relativeRotation);
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
            OnUpdatedAnchors();
            OnPuppetUpdated?.Invoke();
        }

        private void OnUpdatedAnchors()
        {
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
        }

        private void EnableHandTracked()
        {
            SetLivePose(trackedHand);
            _pupettedGripOffset = CalculateGripOffset(); //TODO needed every frame?
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
            _originalGripOffset = CalculateGripOffset();
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
                    boneTransform.localRotation = UnmapRotation(boneTransform,
                        skeleton.Bones[i],
                        _bonesCollection[boneId].rotationOffset);
                }
                else if (trackedHandOffset.id == boneId)
                {

                    Transform boneTransform = trackedHandOffset.transform;
                    boneTransform.localRotation = UnmapRotation(boneTransform,
                        skeleton.Bones[i],
                        trackedHandOffset.rotationOffset);

                    boneTransform.localPosition = trackedHandOffset.positionOffset 
                        + skeleton.Bones[i].Transform.localPosition;

                    TrackedPose = new Pose(boneTransform.position, boneTransform.rotation);
                }
            }

            Quaternion UnmapRotation(Transform boneTransform, OVRBone trackedBone, Vector3 rotationOffset)
            {
                Quaternion offset = Quaternion.Euler(rotationOffset);
                Quaternion desiredRot = trackedBone.Transform.localRotation;
                return offset * desiredRot;
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

        public void LerpBones(HandSnapPose pose, float bonesWeight = 1f)
        {
            InitializeBones();

            if (bonesWeight > 0f)
            {
                foreach (var bone in pose.Bones)
                {
                    BoneId boneId = bone.boneID;
                    if (_bonesCollection.ContainsKey(boneId))
                    {
                        Transform boneTransform = _bonesCollection[boneId].transform;
                        boneTransform.localRotation = Quaternion.Lerp(boneTransform.localRotation, bone.rotation, bonesWeight);
                    }
                }
            }
        }

        public void LerpOffset(HandSnapPose pose, Transform relativeTo, float positionWeight = 1f)
        {
            Pose worldGrip = WorldGripPose;

            Quaternion rotationDif = Quaternion.Inverse(this.transform.rotation) * this.gripPoint.rotation;
            Quaternion desiredRotation = (relativeTo.rotation * pose.relativeGripRot) * rotationDif;
            Quaternion trackedRot = rotationDif * worldGrip.rotation;
            Quaternion finalRot = Quaternion.Lerp(trackedRot, desiredRotation, positionWeight);
            this.transform.rotation = finalRot;

            Vector3 positionDif = this.transform.position - this.gripPoint.position;
            Vector3 desiredPosition = relativeTo.TransformPoint(pose.relativeGripPos) + positionDif;
            Vector3 trackedPosition = worldGrip.position + positionDif;
            Vector3 finalPos = Vector3.Lerp(trackedPosition, desiredPosition, positionWeight);
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