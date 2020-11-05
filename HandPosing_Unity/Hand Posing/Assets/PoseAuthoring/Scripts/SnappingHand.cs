using UnityEngine;
using System.Collections;

using Grabber = Interaction.Grabber;
using Grabbable = Interaction.Grabbable;
using PoseAuthoring.PoseRecording;

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

        private SnapPose _grabSnap;
        private ScoredHandPose _grabPose;

        private float _bonesOverrideFactor;
        private float _offsetOverrideFactor;

        private float _grabStartTime;
        private bool _isGrabbing;
        private Pose _grabOffset;
        private Pose _prevOffset;

        private Coroutine _lastUpdateRoutine;

        private bool IsSnapping
        {
            get
            {
                return _isGrabbing
                    && _grabSnap != null;
            }
        }

        private bool IsSliding
        {
            get
            {
                return IsSnapping
                    &&  grabber.AccotedFlex() <= _grabSnap.slideThresold;
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
            var ghostPose = SnapForGrabbable(grabbable);
            if (ghostPose.HasValue)
            {
                _grabSnap = ghostPose.Value.Item1;
                _grabPose = ghostPose.Value.Item2;
                _offsetOverrideFactor = _bonesOverrideFactor = 1f;

                this.puppet.LerpGripOffset(_grabPose.Pose, _offsetOverrideFactor, _grabSnap.relativeTo);
                _grabOffset = this.puppet.GripOffset;
                _grabStartTime = Time.timeSinceLevelLoad;
                _isGrabbing = true;
            }
        }

        private void GrabEnded(Grabbable grabbable)
        {
            _isGrabbing = false;
            _grabSnap = null;
        }

        private void GrabAttemp(Grabbable grabbable, float amount)
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

        private (SnapPose, ScoredHandPose)? SnapForGrabbable(Grabbable grabbable)
        {
            if (grabbable == null)
            {
                return null;
            }
            SnappableObject snappable = grabbable.Snappable;
            if (snappable != null)
            {
                HandPose userPose = this.puppet.TrackedPose(snappable.transform);
                SnapPose snapPose = snappable.FindBestSnapPose(userPose, out ScoredHandPose bestPose);
                if (snapPose != null)
                {
                    return (snapPose, bestPose);
                }
            }
            return null;
        }
        #endregion


        #region snap lifecycle

        //Occurs before anchors are updated
        private void BeforePuppetUpdate()
        {
            if (IsSliding)
            {
                AttachPhysics(); 
            }
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
                this.puppet.LerpGripOffset(_grabPose.Pose, 1f, _grabSnap.relativeTo);
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
            if (_grabSnap != null)
            {
                this.puppet.LerpBones(_grabPose.Pose.Bones, _bonesOverrideFactor);
                if (_grabSnap.snapsBack)
                {
                    _offsetOverrideFactor = AdjustSnapbackTime(_grabStartTime);
                }
                if (_isGrabbing)
                {
                    this.puppet.LerpGripOffset(_grabOffset, _offsetOverrideFactor);
                    if (IsSliding)
                    {
                        SlidePose();
                    }
                }
                else
                {
                    this.puppet.LerpGripOffset(_grabPose.Pose, _offsetOverrideFactor, _grabSnap.relativeTo);
                }
            }
        }

        private void SlidePose()
        {
            HandPose handPose = this.puppet.TrackedPose(_grabSnap.relativeTo);
            _grabPose = _grabSnap.CalculateBestPlace(handPose, null, _grabPose.Direction);
        }

        private void AttachPhysics()
        {
            Vector3 grabPoint = _grabSnap.NearestInVolume(this.puppet.Grip.position);
            Vector3 gripPos = this.grabber.transform.InverseTransformPoint(this.puppet.Grip.position);
            Joint[] joints = _grabSnap.relativeTo.GetComponents<Joint>();
            foreach (var joint in joints)
            {
                if (joint.connectedBody?.transform == this.grabber.transform)
                {
                    joint.connectedAnchor = gripPos;
                    joint.anchor = joint.transform.InverseTransformPoint(grabPoint);
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
