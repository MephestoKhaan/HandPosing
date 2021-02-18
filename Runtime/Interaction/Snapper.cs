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
        [Tooltip("This MUST implement IGrabNotifier")]
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
        private BaseSnapPoint _grabSnap;
        private ScoredHandPose _grabPose;

        private float _bonesOverrideFactor;
        private float _offsetOverrideFactor;

        private float _grabStartTime;
        private bool _isGrabbing;
        private Pose _trackOffset;

        private Coroutine _lastUpdateRoutine;

        /// <summary>
        /// Indicates if the hand is actually being fully overrided by a snap point.
        /// </summary>
        private bool IsSnapping
        {
            get
            {
                return _isGrabbing
                    && _grabSnap != null;
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
                    float boundedFlex = _grabNotifier.GrabFlexThresold.x
                        + _grabNotifier.CurrentFlex() * (1f - _grabNotifier.GrabFlexThresold.x);
                    return boundedFlex <= _grabSnap.SlideThresold;
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

            _grabNotifier.OnGrabAttemp += GrabAttemp;
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

            _grabNotifier.OnGrabAttemp -= GrabAttemp;
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
            var ghostPose = SnapForGrabbable(grabbable);
            if (ghostPose.HasValue)
            {
                _grabSnap = ghostPose.Value.Item1;
                _grabPose = ghostPose.Value.Item2;
                _offsetOverrideFactor = _bonesOverrideFactor = 1f;

                this.puppet.LerpGripOffset(_grabPose.Pose, _offsetOverrideFactor, _grabSnap.RelativeTo);

                _trackOffset = this.puppet.TrackedHandGripOffset;

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
            _isGrabbing = false;
            _grabSnap = null;
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
            var ghostPose = SnapForGrabbable(grabbable);
            if (ghostPose.HasValue)
            {
                _grabSnap = ghostPose.Value.Item1;
                _grabPose = ghostPose.Value.Item2;
                _offsetOverrideFactor = _bonesOverrideFactor = amount;
            }
            else
            {
                _grabSnap = null;
                _offsetOverrideFactor = _bonesOverrideFactor = 0f;
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
        private (BaseSnapPoint, ScoredHandPose)? SnapForGrabbable(GameObject grabbable)
        {
            if (grabbable == null)
            {
                return null;
            }

            if (grabbable.TryGetComponent<Snappable>(out Snappable snappable))
            {
                HandPose userPose = this.puppet.TrackedPose(snappable.transform);
                BaseSnapPoint snapPose = snappable.FindBestSnapPose(userPose, out ScoredHandPose bestPose);
                if (snapPose != null)
                {
                    return (snapPose, bestPose);
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
                //   AttachPhysics();
            }
        }

        /// <summary>
        /// Life-Cycle method.
        /// Called before the user grab is analyzed.
        /// </summary>
        private void AfterPuppetUpdate()
        {
            AttachToObjectOffseted();
            /*if (this.puppet.IsTrackingHands)
            {
                AttachToObjectOffseted();
            }*/
        }

        /// <summary>
        /// Life-Cycle method.
        /// Called after animations happen, only makes sense if Controllers instead of
        /// Hand-tracing is being used, since that will use an Animator.
        /// </summary>
        private void LateUpdate()
        {
            //if (!this.puppet.IsTrackingHands)
            {
                //AttachToObjectOffseted();
            }

            /*if (IsSnapping)
            {
                this.puppet.LerpGripOffset(_grabPose.Pose, 1f, _grabSnap.RelativeTo);
            }*/
        }

        /// <summary>
        /// Life-Cycle method.
        /// Called right before the objects are drawn.
        /// Ensures the hand is correctly visually attached to the snapped object.
        /// </summary>
        private void OnBeforeRender()
        {
            /*if (IsSnapping)
            {
                this.puppet.LerpGripOffset(_grabPose.Pose, 1f, _grabSnap.RelativeTo);
            }*/
        }

        /// <summary>
        /// Life-Cycle method.
        /// Called after rendering happens and before the next frame.
        /// For physics purposes, makes sure the hand stays at the user position before FixedUpdate
        /// gets called (first of the next frame).
        /// </summary>
        private void OnEndOfFrame()
        {
            if (IsSnapping)
            {
                //this.puppet.LerpGripOffset(_grabPose.Pose, 1f, _grabSnap.RelativeTo);
                //this.puppet.LerpGripOffset(_prevOffset, 1f);
            }
        }
        #endregion

        #region snap methods

        /// <summary>
        /// Overrides the hand position and bonesusing the current snap information 
        /// (if the user is holding, or approaching an object).
        /// </summary>
        private void AttachToObjectOffseted()
        {
            if (_grabSnap != null)
            {
                this.puppet.LerpBones(_grabPose.Pose.Bones, _bonesOverrideFactor);

                if (_isGrabbing)
                {
                    if (_grabSnap.SnapsBack)
                    {
                        _offsetOverrideFactor = AdjustSnapbackTime(_grabStartTime);
                    }

                    this.puppet.LerpGripOffset(_trackOffset, _offsetOverrideFactor, this.transform);

                    if (IsSliding)
                    {
                        //   SlidePose();
                    }
                }
                else
                {
                    this.puppet.LerpGripOffset(_grabPose.Pose, _offsetOverrideFactor, _grabSnap.RelativeTo);
                }
            }
        }

        /// <summary>
        /// Assings a new valid target position/rotation for the hand in the current snapping pose.
        /// </summary>
        private void SlidePose()
        {
            HandPose handPose = this.puppet.TrackedPose(_grabSnap.RelativeTo);
            _grabPose = _grabSnap.CalculateBestPose(handPose, null, _grabPose.Direction);
        }

        /// <summary>
        /// When sliding, reattaches the Joints so the object is held at the new slided position. 
        /// </summary>
        private void AttachPhysics()
        {
            Vector3 grabPoint = _grabSnap.NearestInSurface(this.puppet.Grip.position);
            Vector3 gripPos = this.transform.InverseTransformPoint(this.puppet.Grip.position);
            Joint[] joints = _grabSnap.RelativeTo.GetComponents<Joint>();
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
}
