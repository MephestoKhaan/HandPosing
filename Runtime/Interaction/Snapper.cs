using UnityEngine;
using System.Collections;
using HandPosing.SnapRecording;

namespace HandPosing.Interaction
{
    /// <summary>
    /// This is one of the key Classes of HandPosing, it takes care of overriding the Hand representation
    /// using a HandPuppet, so it snaps to objects when the user is holding them.
    /// 
    /// Since the hand can be updated at different moments on the frame, specially considering the
    /// user can be using hand-tracking (OVRSkeleton executes at -60) or Controllers (Animators execute after Update),
    /// or be using systems like Oculus Rig (could update at Update or FixedUpdate) the order of the calls 
    /// is important, check the Start method to understand the LifeCycle of it.
    /// </summary>
    [DefaultExecutionOrder(10)]
    public class Snapper : MonoBehaviour
    {
        /// <summary>
        /// Component from which to extract the IGrabNotifier. It will inform when the user is
        /// going to perform a grab.
        /// Unity does not allow to assign Interfaces in the inspector, so a general Component is needed.
        /// </summary>
        [SerializeField]
        [Tooltip("It MUST implement IGrabNotifier")]
        private Component grabber;
        /// <summary>
        /// The puppet of the hand, used to override the position/rotation and bones so they adapt
        /// to the desired snap position.
        /// </summary>
        [SerializeField]
        private HandPuppet puppet;

        [Space]
        /// <summary>
        /// When Snap Back is enabled in a SnapPoint, time for the hand to returns to the tracked position.
        /// </summary>
        [SerializeField]
        private float snapbackTime = 0.33f;

        private IGrabNotifier _grabNotifier;

        private ObjectSnappingAddress _snapData;

        private float _fingersSnapFactor;
        private float _allignmentFactor;

        private float _grabStartTime;
        private bool _isGrabbing;
        private Pose _trackOffset;
        private Pose? _prevOffset;

        private Coroutine _lastUpdateRoutine;

        /// <summary>
        /// Indicates if the hand is actually being fully overrided by a snap point.
        /// </summary>
        private bool IsSnapping
        {
            get
            {
                return _isGrabbing
                    && _snapData != null;
            }
        }

        /// <summary>
        /// If True, the hand can slide around the current snap point surface.
        /// This is useful for example when holding a hand-rail, where the hand is holding
        /// the object but can still move around its sliding position.
        /// </summary>
        private bool IsSliding
        {
            get
            {
                if (IsSnapping)
                {
                    float boundedFlex = _grabNotifier.GrabFlexThreshold.x
                        + _grabNotifier.CurrentFlex() * (1f - _grabNotifier.GrabFlexThreshold.x);
                    return boundedFlex <= _snapData.point.SlideThresold;
                }
                return false;
            }
        }

        private void Reset()
        {
            puppet = this.GetComponent<HandPuppet>();
            grabber = this.GetComponent<IGrabNotifier>() as Component;
        }

        private void Awake()
        {
            _grabNotifier = grabber as IGrabNotifier;
        }

        /// <summary>
        /// Initialization of the LifeCycle of callbacks to override the hand pose.
        /// </summary>
        private void Start()
        {
            puppet.OnPoseBeforeUpdate += BeforePuppetUpdate;

            _grabNotifier.OnGrabAttempt += GrabAttemp;
            _grabNotifier.OnGrabStarted += GrabStarted;
            _grabNotifier.OnGrabEnded += GrabEnded;

            puppet.OnPoseUpdated += AfterPuppetUpdate;
            Application.onBeforeRender += OnBeforeRender;
            if (_lastUpdateRoutine == null)
            {
                _lastUpdateRoutine = StartCoroutine(LastUpdateLoop());
            }
        }

        private void OnDestroy()
        {
            puppet.OnPoseBeforeUpdate -= BeforePuppetUpdate;

            _grabNotifier.OnGrabAttempt -= GrabAttemp;
            _grabNotifier.OnGrabStarted -= GrabStarted;
            _grabNotifier.OnGrabEnded -= GrabEnded;

            puppet.OnPoseUpdated -= AfterPuppetUpdate;
            Application.onBeforeRender -= OnBeforeRender;
            if (_lastUpdateRoutine != null)
            {
                StopCoroutine(_lastUpdateRoutine);
                _lastUpdateRoutine = null;
            }
        }

        private static YieldInstruction _endOfFrame = new WaitForEndOfFrame();
        /// <summary>
        /// Extra Update loop that gets called even after rendering has happened
        /// </summary>
        /// <returns></returns>
        private IEnumerator LastUpdateLoop()
        {
            while (true)
            {
                yield return _endOfFrame;
                OnEndOfFrame();
            }
        }

        #region grabber callbacks

        /// <summary>
        /// From IGrabNotifier, called when the hand starts grabbing and object.
        /// Finds the best snap point and overrides the hand to it.
        /// </summary>
        /// <param name="grabbable">The grabbed object.</param>
        private void GrabStarted(GameObject grabbable)
        {
            ObjectSnappingAddress address = SnapForGrabbable(grabbable);
            if (address != null)
            {
                _snapData = address;
                _allignmentFactor = _fingersSnapFactor = 1f;
                this.puppet.LerpGripOffset(_snapData.pose.Pose, _allignmentFactor, _snapData.point.RelativeTo);

                _trackOffset = this.puppet.TrackedGripOffset;
                _grabStartTime = Time.timeSinceLevelLoad;
                _isGrabbing = true;
            }
        }

        /// <summary>
        /// From IGrabNotifier, called when the hand releases an object.
        /// Remove the overrides from the hand so it is controlled directly by the user.
        /// </summary>
        /// <param name="grabbable">The released object.</param>
        private void GrabEnded(GameObject grabbable)
        {
            this.puppet.LerpGripOffset(Pose.identity, 0f, this.transform);
            _snapData = null;
            _isGrabbing = false;
        }

        /// <summary>
        /// From IGrabNotifier. Called every frame as the user approaches a grabbable while
        /// performing the grabbing pose.
        /// Depending on how closed the grab-gesture is, the hand is interpolated more from
        /// the user-data to the snap-pose data, generating an "approach" animation directly
        /// controlled by the gesture.
        /// </summary>
        /// <param name="grabbable">The object that is intended to be grabbed.</param>
        /// <param name="amount">How much the user is performing the grabbing pose (normalised)</param>
        private void GrabAttemp(GameObject grabbable, float amount)
        {
            ObjectSnappingAddress address = SnapForGrabbable(grabbable);
            if (address != null)
            {
                _snapData = address;
                _allignmentFactor = _fingersSnapFactor = amount;
            }
            else
            {
                _snapData = null;
                _allignmentFactor = _fingersSnapFactor = 0f;
            }
        }

        /// <summary>
        /// Calculate the best pre-recorded snap-point to grab an object.
        /// </summary>
        /// <param name="grabbable">The snappable object.</param>
        /// <returns>
        /// If the snappable is object is valid, the best SnapPoint to grab it alongside the
        /// Hand-Pose to use when grabbing at that position.
        /// </returns>
        public ObjectSnappingAddress SnapForGrabbable(GameObject grabbable)
        {
            if (grabbable != null
                && grabbable.TryGetComponent<Snappable>(out Snappable snappable))
            {
                HandPose userPose = this.puppet.TrackedPose(snappable.transform);
                BaseSnapPoint snapPose = snappable.FindBestSnapPose(userPose, out ScoredHandPose bestPose);
                if (snapPose != null)
                {
                    return new ObjectSnappingAddress(snappable, snapPose, bestPose);
                }
            }
            return null;
        }
        #endregion


        #region snap lifecycle

        /// <summary>
        /// Life-Cycle method.
        /// Called before anchors are updated.
        /// </summary>
        private void BeforePuppetUpdate()
        {
            if (IsSliding)
            {
                AttachPhysics();
            }
        }

        /// <summary>
        /// Life-Cycle method.
        /// Called before the user grab is analyzed.
        /// </summary>
        private void AfterPuppetUpdate()
        {
            AlignSnappable(false);
        }

        /// <summary>
        /// Life-Cycle method.
        /// Called after animations happen, only makes sense if Controllers instead of
        /// Hand-tracing is being used, since that will use an Animator.
        /// </summary>
        private void LateUpdate()
        {
            if (!this.puppet.IsTrackingHands)
            {
                AlignSnappable(true);
            }
        }

        /// <summary>
        /// Life-Cycle method.
        /// Called right before the objects are drawn.
        /// Ensures the hand is correctly visually attached to the snapped object.
        /// </summary>
        private void OnBeforeRender()
        {
            if (IsSnapping)
            {
                _prevOffset = this.transform.GetPose();
                this.puppet.LerpGripOffset(_snapData.pose.Pose, 1f, _snapData.point.RelativeTo);
            }
        }

        /// <summary>
        /// Life-Cycle method.
        /// Called after rendering happens and before the next frame.
        /// For physics purposes, makes sure the hand stays at the user position before FixedUpdate
        /// gets called (first of the next frame).
        /// </summary>
        private void OnEndOfFrame()
        {
            if (_prevOffset.HasValue)
            {
                this.transform.SetPose(_prevOffset.Value);
                _prevOffset = null;
            }
        }
        #endregion

        #region snap methods

        /// <summary>
        /// Aligns Object and Hand using different techniques based on the SnapMode.
        /// The hand can move towards an object, or the object towards the hand, and the
        /// fingers should curl to wrap it.
        /// </summary>
        private void AlignSnappable(bool fingersOnly = false)
        {
            if (_snapData != null)
            {
                this.puppet.LerpBones(_snapData.pose.Pose.Bones, _fingersSnapFactor);
                if(fingersOnly)
                {
                    return;
                }
                SnapType snapMode = _snapData.point.SnapMode;
                float moveFactor = _allignmentFactor;
                if (_isGrabbing)
                {
                    if(snapMode == SnapType.MoveHand
                        || snapMode == SnapType.MoveHandReturn)
                    {
                        if (snapMode == SnapType.MoveHandReturn)
                        {
                            moveFactor = AdjustSnapbackTime(_grabStartTime);
                        }
                        this.puppet.LerpGripOffset(_trackOffset, moveFactor, this.transform);
                    }
                    if (IsSliding)
                    {
                        SlidePose();
                    }
                }
                else
                {
                    if(snapMode == SnapType.MoveObject)
                    {
                        _snapData.LerpObjectTowarsHand(puppet.TrackedGripPose, moveFactor);
                    }
                    else
                    {
                        this.puppet.LerpGripOffset(_snapData.pose.Pose, moveFactor, _snapData.point.RelativeTo);
                    }
                }
            }
        }

        /// <summary>
        /// Assings a new valid target position/rotation for the hand in the current snapping pose.
        /// </summary>
        private void SlidePose()
        {
            HandPose handPose = this.puppet.TrackedPose(_snapData.point.RelativeTo);
            _snapData.pose = _snapData.point.CalculateBestPose(handPose, null, _snapData.pose.Direction);
        }

        /// <summary>
        /// When sliding, reattaches the Joints so the object is held at the new slided position. 
        /// </summary>
        private void AttachPhysics()
        {
            Vector3 grabPoint = _snapData.point.NearestInSurface(this.puppet.Grip.position);
            Vector3 gripPos = this.transform.InverseTransformPoint(this.puppet.Grip.position);
            Joint[] joints = _snapData.point.RelativeTo.GetComponents<Joint>();
            foreach (var joint in joints)
            {
                if (joint.connectedBody?.transform == this.transform)
                {
                    joint.connectedAnchor = gripPos;
                    joint.anchor = joint.transform.InverseTransformPoint(grabPoint);
                }
            }
        }

        /// <summary>
        /// Calculates the factor for snapping back the hand from the object to the
        /// user-hand position.
        /// </summary>
        /// <param name="grabStartTime">Time at which the grab started</param>
        /// <returns>A normalised value indicating how much to override the hand pose</returns>
        private float AdjustSnapbackTime(float grabStartTime)
        {
            return 1f - Mathf.Clamp01((Time.timeSinceLevelLoad - grabStartTime) / snapbackTime);
        }
        #endregion
    }


    public class ObjectSnappingAddress
    {
        public Snappable snappable;
        public BaseSnapPoint point;
        public ScoredHandPose pose;
        public Pose originalPose;

        public ObjectSnappingAddress(Snappable snappable, BaseSnapPoint point, ScoredHandPose pose)
        {
            this.snappable = snappable;
            this.point = point;
            this.pose = pose;
            this.originalPose = snappable.transform.GetPose();
        }

        public void LerpObjectTowarsHand(Pose gripPose, float t)
        {
            Pose targetPose = PoseUtils.Lerp(originalPose, AllignedObjectPose(gripPose), t);
            point.RelativeTo.SetPose(targetPose);
        }

        public void MoveObjectTowardsHand(Pose gripPose, float step)
        {
            Pose current = point.RelativeTo.GetPose();
            Pose desiredPose = AllignedObjectPose(gripPose);

            Vector3 difference = desiredPose.position - current.position;
            Pose targetPose = new Pose(
                current.position + difference.normalized * Mathf.Min(step, difference.magnitude),
                Quaternion.RotateTowards(current.rotation, desiredPose.rotation, step * Mathf.Rad2Deg * 10f));

            point.RelativeTo.SetPose(targetPose);
        }

        public Pose AllignedObjectPose(Pose gripPose)
        {
            Pose offset = pose.Pose.relativeGrip.Inverse();
            return new Pose(
                gripPose.position + point.RelativeTo.rotation * offset.position,
                gripPose.rotation * offset.rotation);
        }
    }
}
