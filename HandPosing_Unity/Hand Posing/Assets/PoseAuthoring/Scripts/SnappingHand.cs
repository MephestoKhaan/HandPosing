using UnityEngine;
using System.Collections;

using Grabber = Interaction.Grabber;
using Grabbable = Interaction.Grabbable;
using System.Collections.Generic;

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

        private bool IsSnapping
        {
            get
            {
                return _isGrabbing
                    && _grabbedGhost != null;
            }
        }

        private Coroutine _lastUpdateRoutine;

        private void Start()
        {
            grabber.OnGrabAttemp += GrabAttemp;
            grabber.OnGrabStarted += GrabStarted;
            grabber.OnGrabEnded += GrabEnded;

            Application.onBeforeRender += VisuallyAttach;
            puppet.OnPoseWillUpdate += SnapSlide;
            puppet.OnPoseUpdated += AfterPuppetUpdate;
            if (_lastUpdateRoutine == null)
            {
                _lastUpdateRoutine = StartCoroutine(LastUpdate());
            }
        }

        private void OnDestroy()
        {
            grabber.OnGrabAttemp -= GrabAttemp;
            grabber.OnGrabStarted -= GrabStarted;
            grabber.OnGrabEnded -= GrabEnded;

            Application.onBeforeRender -= VisuallyAttach;
            puppet.OnPoseWillUpdate -= SnapSlide;
            puppet.OnPoseUpdated -= AfterPuppetUpdate;
            if (_lastUpdateRoutine != null)
            {
                StopCoroutine(_lastUpdateRoutine);
                _lastUpdateRoutine = null;
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

        private static YieldInstruction _endOfFrame = new WaitForEndOfFrame();
        private IEnumerator LastUpdate()
        {
            while (true)
            {
                yield return _endOfFrame;
                UndoVisualAttach();
            }
        }

        private void UndoVisualAttach()
        {
            if (IsSnapping)
            {
                this.puppet.LerpGripOffset(_prevOffset, 1f);
            }
        }

        private void VisuallyAttach()
        {
            if (IsSnapping)
            {
                _prevOffset = this.puppet.GripOffset;

                if (false)//_grabbedGhost.Snappable.HandSlides)
                {
                    HandSnapPose handPose = this.puppet.TrackedPose(_grabbedGhost.RelativeTo);
                    ScoredSnapPose bestPlace = _grabbedGhost.CalculateBestPlace(handPose, this.puppet.Grip.GetPose(), _grabPose.Direction);
                    HandSnapPose ghostPose = bestPlace.SnapPose;
                    this.puppet.LerpGripOffset(ghostPose, 1f, _grabbedGhost.RelativeTo);

                }
                else
                {
                    this.puppet.LerpGripOffset(_grabPose.SnapPose, 1f, _grabbedGhost.RelativeTo);
                }
            }
        }

        private void LateUpdate()
        {
            if (!this.puppet.IsTrackingHands)
            {
                AttachToObjectOffseted();
            }
        }

        bool _physicsUpdated;

        private void FixedUpdate()
        {
            if (_isGrabbing&& _grabbedGhost.Snappable.HandSlides)
            {
                Joint[] joints = _grabbedGhost.RelativeTo.GetComponents<Joint>();
                Joint grabJoint = null;
                Rigidbody rb = this.puppet.GetComponent<Rigidbody>();
                for(int i = joints.Length-1; i >= 0; i--)
                {
                    if(joints[i].connectedBody == rb)
                    {
                        grabJoint = joints[i];
                        break;
                    }
                }

                if (grabJoint != null)
                {
                    grabJoint.anchor = _grabPose.SnapPose.relativeGripPos;
                    grabJoint.connectedAnchor = this.puppet.transform.InverseTransformPoint(this.puppet.Grip.position);
                }
            }
            _physicsUpdated = true;
        }

        private void SnapSlide()
        {
            if(!_physicsUpdated)
            {
                return;
            }
            _physicsUpdated = false;
            if (_isGrabbing 
                && _grabbedGhost.Snappable.HandSlides)
            {
                HandSnapPose handPose = this.puppet.TrackedPose(_grabbedGhost.RelativeTo);
                _grabPose = _grabbedGhost.CalculateBestPlace(handPose, this.puppet.Grip.GetPose(), _grabPose.Direction);
                
            }
        }

        private void AfterPuppetUpdate()
        {
            if (this.puppet.IsTrackingHands)
            {
                AttachToObjectOffseted();
            }
        }

        private void AttachToObjectOffseted()
        {
            if (_grabbedGhost != null)
            {
                this.puppet.LerpBones(_grabPose.SnapPose, _bonesOverrideFactor);
                if (_snapsBack)
                {
                    _offsetOverrideFactor = AdjustSnapbackTime(_grabStartTime);
                }
                if (_isGrabbing)
                {
                    //this.puppet.LerpGripOffset(_grabOffset, _offsetOverrideFactor);
                    //SnapSlide();
                }
                else
                {
                    this.puppet.LerpGripOffset(_grabPose.SnapPose, _offsetOverrideFactor, _grabbedGhost.RelativeTo);
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
