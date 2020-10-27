using UnityEngine;
using System.Collections;

using Grabber = Interaction.Grabber;
using Grabbable = Interaction.Grabbable;

namespace PoseAuthoring
{
    [DefaultExecutionOrder(10)]
    public class SnappingHand : MonoBehaviour
    {
        [SerializeField]
        private Grabber grabber;
        [SerializeField]
        private HandPuppet puppet;
        [Space]
        [SerializeField]
        private float snapbackTime = 0.33f;

        private HandGhost _grabbedGhost;
        private ScoredSnapPose _grabPose;

        private float _bonesOverrideFactor;
        private float _offsetOverrideFactor;

        private float _grabStartTime;
        private bool _isGrabbing;
        private bool _snapsBack;
        private Pose _grabOffset;
        private Pose _prevOffset;

        private Coroutine _lastUpdateRoutine;

        private bool IsSnapping
        {
            get
            {
                return _isGrabbing
                    && _grabbedGhost != null;
            }
        }

        private bool IsSliding
        {
            get
            {
                return _isGrabbing 
                    && _grabbedGhost != null 
                    && _grabbedGhost.Snappable.HandSlides;
            }
        }


        private void Start()
        {
            grabber.OnGrabAttemp += GrabAttemp;
            grabber.OnGrabStarted += GrabStarted;
            grabber.OnGrabEnded += GrabEnded;

            puppet.OnPoseBeforeUpdate += BeforePuppetUpdate;
            puppet.OnPoseUpdated += AfterPuppetUpdate;
            Application.onBeforeRender += OnBeforeRender;
            if (_lastUpdateRoutine == null)
            {
                _lastUpdateRoutine = StartCoroutine(LastUpdateLoop());
            }
        }

        private void OnDestroy()
        {
            grabber.OnGrabAttemp -= GrabAttemp;
            grabber.OnGrabStarted -= GrabStarted;
            grabber.OnGrabEnded -= GrabEnded;

            puppet.OnPoseBeforeUpdate -= BeforePuppetUpdate;
            puppet.OnPoseUpdated -= AfterPuppetUpdate;
            Application.onBeforeRender -= OnBeforeRender;
            if (_lastUpdateRoutine != null)
            {
                StopCoroutine(_lastUpdateRoutine);
                _lastUpdateRoutine = null;
            }
        }

        private static YieldInstruction _endOfFrame = new WaitForEndOfFrame();
        private IEnumerator LastUpdateLoop()
        {
            while (true)
            {
                yield return _endOfFrame;
                OnEndOfFrame();
            }
        }

        #region grabber callbacks

        private void GrabStarted(Grabbable grabbable)
        {
            var ghostPose = GhostForGrabbable(grabbable);
            if (ghostPose.HasValue)
            {
                _grabbedGhost = ghostPose.Value.Item1;
                _grabPose = ghostPose.Value.Item2;
                _offsetOverrideFactor = _bonesOverrideFactor = 1f;

                this.puppet.LerpGripOffset(_grabPose.SnapPose, _offsetOverrideFactor, _grabbedGhost.RelativeTo);
                _grabOffset = this.puppet.GripOffset;

                _snapsBack = _grabbedGhost.Snappable.HandSnapBacks;
                _grabStartTime = Time.timeSinceLevelLoad;
                _isGrabbing = true;
            }
        }

        private void GrabEnded(Grabbable grabbable)
        {
            _isGrabbing = false;
            _grabbedGhost = null;
            _snapsBack = false;
        }

        private void GrabAttemp(Grabbable grabbable, float amount)
        {
            _snapsBack = false;
            var ghostPose = GhostForGrabbable(grabbable);
            if (ghostPose.HasValue)
            {
                _grabbedGhost = ghostPose.Value.Item1;
                _grabPose = ghostPose.Value.Item2;
                _offsetOverrideFactor = _bonesOverrideFactor = amount;
            }
            else
            {
                _grabbedGhost = null;
                _offsetOverrideFactor = _bonesOverrideFactor = 0f;
            }
        }

        private (HandGhost, ScoredSnapPose)? GhostForGrabbable(Grabbable grabbable)
        {
            if (grabbable == null)
            {
                return null;
            }
            SnappableObject snappable = grabbable.Snappable;
            if (snappable != null)
            {
                HandSnapPose userPose = this.puppet.TrackedPose(snappable.transform);
                HandGhost ghost = snappable.FindBestGhost(userPose, out ScoredSnapPose bestPose);
                if (ghost != null)
                {
                    return (ghost, bestPose);
                }
            }
            return null;
        }
        #endregion


        #region snap lifecycle

        private bool _physicsUpdated;
        private void FixedUpdate()
        {
            _physicsUpdated = true;
        }

        //Occurs before anchors are updated
        private void BeforePuppetUpdate()
        {
            if(_physicsUpdated && IsSliding)
            {
                ReAttachPhysics();
            }
            _physicsUpdated = false;
        }


        //Occurs before grabbing
        private void AfterPuppetUpdate()
        {
            if (this.puppet.IsTrackingHands)
            {
                AttachToObjectOffseted();
            }
        }

        //Occurs after animations
        private void LateUpdate()
        {
            if (!this.puppet.IsTrackingHands)
            {
                AttachToObjectOffseted();
            }
        }

        private void OnBeforeRender()
        {
            if (IsSnapping)
            {
                _prevOffset = this.puppet.GripOffset;
                this.puppet.LerpGripOffset(_grabPose.SnapPose, 1f, _grabbedGhost.RelativeTo);
            }
        }

        private void OnEndOfFrame()
        {
            if (IsSnapping)
            {
                this.puppet.LerpGripOffset(_prevOffset, 1f);
            }
        }
        #endregion

        #region snap methods

        private void AttachToObjectOffseted()
        {
            if (_grabbedGhost != null)
            {
                this.puppet.LerpBones(_grabPose.SnapPose.Bones, _bonesOverrideFactor);
                if (_snapsBack)
                {
                    _offsetOverrideFactor = AdjustSnapbackTime(_grabStartTime);
                }
                if (_isGrabbing)
                {
                    this.puppet.LerpGripOffset(_grabOffset, _offsetOverrideFactor);
                    SnapSlide();
                }
                else
                {
                    this.puppet.LerpGripOffset(_grabPose.SnapPose, _offsetOverrideFactor, _grabbedGhost.RelativeTo);
                }
            }
        }

        private void SnapSlide()
        {
            if (IsSliding)
            {
                HandSnapPose handPose = this.puppet.TrackedPose(_grabbedGhost.RelativeTo);
                _grabPose = _grabbedGhost.CalculateBestPlace(handPose, _grabPose.Direction);
            }
        }

        private void ReAttachPhysics()
        {
            Joint[] joints = _grabbedGhost.RelativeTo.GetComponents<Joint>();
            foreach (var joint in joints)
            {
                if (joint.connectedBody?.transform == this.grabber.transform)
                {
                    joint.anchor = joint.transform.InverseTransformPoint(this.grabber.transform.position);
                }
            }
        }

        private float AdjustSnapbackTime(float grabStartTime)
        {
            return 1f - Mathf.Clamp01((Time.timeSinceLevelLoad - grabStartTime) / snapbackTime);
        }
        #endregion
    }
}
