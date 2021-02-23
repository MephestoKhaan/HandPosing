using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HandPosing
{
    /// <summary>
    /// This class controls the representation of a hand (typically a skin-mesh renderer) by
    /// moving its position/rotation and the rotations of the bones that compose it.
    /// 
    /// The data that drives this puppet could come from a hand-tracking system but sometimes
    /// it is overriden by the snapping system. 
    /// In that matter, it is also important to note the difference between tracked data and its representation,
    /// sometimes they might differ when the representation is overriden.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class HandPuppet : MonoBehaviour
    {
        /// <summary>
        /// The hand-tracking data provider.
        /// </summary>
        [SerializeField]
        [Tooltip("The hand-tracking data provider")]
        private SkeletonDataProvider skeleton;
        /// <summary>
        /// Callbacks indicating when the hand tracking has updated.
        /// Not mandatory.
        /// </summary>
        [SerializeField]
        [Tooltip("Callbacks indicating when the hand tracking has updated. Not mandatory.")]
        private AnchorsUpdateNotifier updateNotifier;
        /// <summary>
        /// Transform for the grip point of the hand. Tipically at the centre of the hand.
        /// It is important that the grip point is well alligned with the palm.
        /// </summary>
        [SerializeField]
        [Tooltip("Transform for the grip point of the hand. Should be at the centre of the hand and alligned with it.")]
        private Transform gripPoint;
        /// <summary>
        /// Handeness of the hand.
        /// </summary>
        [SerializeField]
        [Tooltip("Is this a right or a left hand?")]
        private Handeness handeness;
        /// <summary>
        /// Should the Hand adjusts its size to the user's
        /// </summary>
        [SerializeField]
        [Tooltip("Should the hand size adjust to the user (when using hand-tracking)")]
        private bool autoAdjustScale;

        /// <summary>
        /// Offset of the hand when using hand-tracking.
        /// Note that the default position is for when using controllers.
        /// </summary>
        [SerializeField]
        [Tooltip("Offset of the hand (from the anchor) when using hand-tracking instead of controllers.")]
        private HandMap trackedHandOffset;
        /// <summary>
        /// Bones of the hand and their relative rotations compared to hand-tracking.
        /// </summary>
        [SerializeField]
        private List<BoneMap> boneMaps;

        /// <summary>
        /// Callback triggered when the user changes control mode to Hand Tracking.
        /// </summary>
        [SerializeField]
        private UnityEvent OnUsingHands;
        /// <summary>
        /// Callback triggered when the user changes control mode to Controllers.
        /// </summary>
        [SerializeField]
        private UnityEvent OnUsingControllers;

        /// <summary>
        /// General getter for the bones of the hand.
        /// </summary>
        public List<BoneMap> Bones
        {
            get
            {
                return boneMaps;
            }
        }

        /// <summary>
        /// Current scale of the represented hand.
        /// </summary>
        public float Scale
        {
            get
            {
                return this.transform.localScale.x;
            }
            private set
            {
                this.transform.localScale = Vector3.one * value;
            }
        }

        /// <summary>
        /// General getter for the grip point of the hand.
        /// </summary>
        public Transform Grip
        {
            get
            {
                return gripPoint;
            }
        }

        /// <summary>
        /// True if the user is using hand-tracking, false if using controllers.
        /// </summary>
        public bool IsTrackingHands
        {
            get
            {
                return _trackingHands;
            }
        }

        /// <summary>
        /// Relative pose of the representated grip from the tracked hand.
        /// </summary>
        public Pose TrackedGripOffset
        {
            get
            {
                return PoseUtils.RelativeOffset(Grip.GetPose(), _trackedPose);
            }
        } 


        /// <summary>
        /// World rotation of the tracked 
        /// </summary>
        public Pose TrackedGripPose
        {
            get
            {
                if (!_offsetInitialised)
                {
                    CacheGripOffsets();
                }
                Pose offset = _originalGripOffset;// _trackingHands ? _pupettedGripOffset : _originalGripOffset;
                offset.position = offset.position * Scale;
                return this.transform.GlobalPose(offset);
            }
        }

        /// <summary>
        /// Callback before applying the puppeting to the hand representation.
        /// </summary>
        public System.Action OnPoseBeforeUpdate;
        /// <summary>
        /// Callback after applying the puppeting to the hand representation.
        /// </summary>
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
        private Pose _trackedPose;
        //private Pose _pupettedGripOffset;
        private bool _offsetInitialised = false;
        private bool _usingUpdateNotifier;
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
            _originalGripOffset = this.transform.RelativeOffset(this.gripPoint);
            //_pupettedGripOffset = OffsetedGripPose();
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

        private void Update()
        {
            OnPoseBeforeUpdate?.Invoke();
            if (!_usingUpdateNotifier)
            {
                UpdateHandPose();
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
                Scale = autoAdjustScale ? (skeleton.HandScale ?? 1f) : 1f;
                OnUsingHands?.Invoke();
            }

            SetLivePose(skeleton);
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

        private void SetLivePose(SkeletonDataProvider skeletonData)
        {
            BoneRotation[] fingers = skeletonData.Fingers;
            BoneRotation hand = skeletonData.Hand;

            for (int i = 0; i < fingers.Length; ++i)
            {
                BoneId boneId = fingers[i].boneID;
                if (BonesCache.ContainsKey(boneId))
                {
                    Transform boneTransform = BonesCache[boneId].transform;
                    Quaternion desiredRot = BonesCache[boneId].RotationOffset * fingers[i].rotation;
                    boneTransform.localRotation = desiredRot;
                }
            }

            Pose rootPose = new Pose(trackedHandOffset.positionOffset
                    + trackedHandOffset.RotationOffset * hand.position,
                    trackedHandOffset.RotationOffset * hand.rotation);
            _trackedPose = rootPose;
            this.transform.SetPose(rootPose, Space.World);
        }

        #region pose lerping

        /// <summary>
        /// Rotates the bones of the hand towards the given ones using interpolation.
        /// </summary>
        /// <param name="bones">The target bone rotations.</param>
        /// <param name="weight">Interpolation factor for the bones. 0 for staying with the current values, 1 for fully overriding with the new ones.</param>
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
                        Quaternion targetRot = BonesCache[boneId].RotationOffset * bone.rotation;
                        boneTransform.localRotation = Quaternion.Lerp(boneTransform.localRotation, targetRot, weight);
                    }
                }
            }
        }

        /// <summary>
        /// Moves the hand positing/rotation towards the given Grip pose using interpolation.
        /// The target pose is specified in local units from a reference transform.
        /// </summary>
        /// <param name="pose">The relative target position for the grip point of the hand</param>
        /// <param name="weight">Interpolation factor, 0 for not changing the hand, 1 for fully alligning the grip point with the given pose.</param>
        /// <param name="relativeTo">The reference transform in which the pose is provided.</param>
        public void LerpGripOffset(HandPose pose, float weight, Transform relativeTo)
        {
            LerpGripOffset(pose.relativeGrip, weight, relativeTo);
        }

        /// <summary>
        /// Moves the hand at a given pose towards the given Grip pose using interpolation.
        /// The target pose is specified in local units from a reference transform.
        /// </summary>
        /// <param name="pose">The relative target position for the grip point of the hand</param>
        /// <param name="weight">Interpolation factor, 0 for not changing the hand, 1 for fully alligning the grip point with the given pose.</param>
        /// <param name="relativeTo">The reference transform in which the pose is provided.</param>
        public void LerpGripOffset(Pose pose, float weight, Transform relativeTo)
        {
            Pose fromGrip = this.gripPoint.GetPose();
            Pose toGrip = (relativeTo??this.transform).GlobalPose(pose);
            Pose targetGrip = PoseUtils.Lerp(fromGrip, toGrip, weight);

            Pose inverseGrip = this.gripPoint.RelativeOffset(this.transform);
            Pose targetPose = PoseUtils.Multiply(targetGrip, inverseGrip);
            this.trackedHandOffset.transform.SetPose(targetPose);

        }
        #endregion

        #region currentPoses

        /// <summary>
        /// Gets the current represented hand pose oriented in relation to a given object.
        /// </summary>
        /// <param name="relativeTo">The object from which to obtain the pose. Typically an object that is going to be grabbed.</param>
        /// <param name="includeBones">True for including all the bones data in the result (default False)</param>
        /// <returns>A new HandPose</returns>
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
                    Quaternion rotation = boneMap.TrackedRotation;
                    pose.Bones.Add(new BoneRotation() { boneID = boneMap.id, rotation = rotation });
                }
            }
            return pose;
        }

        #endregion
    }
}