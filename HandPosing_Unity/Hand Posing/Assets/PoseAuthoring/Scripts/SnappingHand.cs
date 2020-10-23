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
        private HandSnapPose _poseInGhost;

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
                _poseInGhost = ghostPose.Value.Item2;
                _offsetOverrideFactor = _bonesOverrideFactor = 1f;

                this.puppet.LerpGripOffset(_poseInGhost, _offsetOverrideFactor, _grabbedGhost.RelativeTo);
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
                _poseInGhost = ghostPose.Value.Item2;
                _offsetOverrideFactor = _bonesOverrideFactor = amount;
            }
            else
            {
                _grabbedGhost = null;
                _offsetOverrideFactor = _bonesOverrideFactor = 0f;
            }
        }

        private (HandGhost, HandSnapPose)? GhostForGrabbable(Grabbable grabbable)
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
                    HandSnapPose ghostPose = ghost.AdjustPlace(bestPose.SnapPose);
                    return (ghost, ghostPose);
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

                if(_grabbedGhost.Snappable.HandSlides)
                {
                    HandSnapPose handPose = this.puppet.TrackedPose(_grabbedGhost.RelativeTo);
                    ScoredSnapPose bestPlace = _grabbedGhost.CalculateBestPlace(handPose, this.puppet.Grip.GetPose());
                    HandSnapPose ghostPose = _grabbedGhost.AdjustPlace(bestPlace.SnapPose);
                    this.puppet.LerpGripOffset(ghostPose, 1f, _grabbedGhost.RelativeTo);
                }
                else
                {
                    this.puppet.LerpGripOffset(_poseInGhost, 1f, _grabbedGhost.RelativeTo); 
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
                this.puppet.LerpBones(_poseInGhost, _bonesOverrideFactor);
                if (_snapsBack)
                {
                    _offsetOverrideFactor = AdjustSnapbackTime(_grabStartTime);
                }
                if (_isGrabbing)
                {
                    this.puppet.LerpGripOffset(_grabOffset, _offsetOverrideFactor);
                }
                else
                {
                    this.puppet.LerpGripOffset(_poseInGhost, _offsetOverrideFactor, _grabbedGhost.RelativeTo);
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
