using UnityEngine;
using System.Collections;

using PoseAuthoring.PoseRecording;
using PoseAuthoring.Adapters;

namespace PoseAuthoring
{
    [DefaultExecutionOrder(10)]
    public class SnappingHand : MonoBehaviour
    {
        [SerializeField]
        private GrabNotifier grabNotifier;
        [SerializeField]
        private HandPuppet puppet;
        [Space]
        [SerializeField]
        private float snapbackTime = 0.33f;

        private SnapPoint _grabSnap;
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
                    && grabNotifier.AccotedFlex() <= _grabSnap.SlideThresold;
            }
        }


        private void Start()
        {
            grabNotifier.OnGrabAttemp += GrabAttemp;
            grabNotifier.OnGrabStarted += GrabStarted;
            grabNotifier.OnGrabEnded += GrabEnded;

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
            grabNotifier.OnGrabAttemp -= GrabAttemp;
            grabNotifier.OnGrabStarted -= GrabStarted;
            grabNotifier.OnGrabEnded -= GrabEnded;

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

        private void GrabStarted(GameObject grabbable)
        {
            var ghostPose = SnapForGrabbable(grabbable);
            if (ghostPose.HasValue)
            {
                _grabSnap = ghostPose.Value.Item1;
                _grabPose = ghostPose.Value.Item2;
                _offsetOverrideFactor = _bonesOverrideFactor = 1f;

                this.puppet.LerpGripOffset(_grabPose.Pose, _offsetOverrideFactor, _grabSnap.RelativeTo);
                _grabOffset = this.puppet.GripOffset;
                _grabStartTime = Time.timeSinceLevelLoad;
                _isGrabbing = true;
            }
        }

        private void GrabEnded(GameObject grabbable)
        {
            _isGrabbing = false;
            _grabSnap = null;
        }

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

        private (SnapPoint, ScoredHandPose)? SnapForGrabbable(GameObject grabbable)
        {
            if (grabbable == null)
            {
                return null;
            }
            SnappableObject snappable = grabbable.GetComponent<SnappableObject>();
            if (snappable != null)
            {
                HandPose userPose = this.puppet.TrackedPose(snappable.transform);
                SnapPoint snapPose = snappable.FindBestSnapPose(userPose, out ScoredHandPose bestPose);
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
                this.puppet.LerpGripOffset(_grabPose.Pose, 1f, _grabSnap.RelativeTo);
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
                
                if (_isGrabbing)
                {
                    if (_grabSnap.SnapsBack)
                    {
                        _offsetOverrideFactor = AdjustSnapbackTime(_grabStartTime);
                    }
                    this.puppet.LerpGripOffset(_grabOffset, _offsetOverrideFactor);
                    if (IsSliding)
                    {
                        SlidePose();
                    }
                }
                else
                {
                    this.puppet.LerpGripOffset(_grabPose.Pose, _offsetOverrideFactor, _grabSnap.RelativeTo);
                }
            }
        }

        private void SlidePose()
        {
            HandPose handPose = this.puppet.TrackedPose(_grabSnap.RelativeTo);
            _grabPose = _grabSnap.CalculateBestPose(handPose, null, _grabPose.Direction);
        }

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

        private float AdjustSnapbackTime(float grabStartTime)
        {
            return 1f - Mathf.Clamp01((Time.timeSinceLevelLoad - grabStartTime) / snapbackTime);
        }
        #endregion
    }
}
