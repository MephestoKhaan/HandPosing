using HandPosing.Interaction;
using System;
using System.Collections.Generic;
using UnityEngine;

using GrabEngine;

namespace HandPosing.OVRIntegration
{
    /// <summary>
    /// Custom grabber for the Oculus Plugin
    /// This Grabber supports grabbing with both Oculus Hand tracking, using Pinch gesture
    /// and grabbing using Oculus Touch controllers, using the Primary Hand Trigger.
    /// </summary>
    public class GrabberHybridOVR : BaseGrabber
    {
        [Header("OVR dependencies")]
        [SerializeField]
        private OVRHand trackedHand;
        [SerializeField]
        private Transform handAnchor;
        /// <summary>
        /// Release (X) and Grab (Y) values for the controller trigger.
        /// </summary>
        [SerializeField]
        [Tooltip("Release (X) and Grab (Y) values for the controller trigger.")]
        private Vector2 grabThresoldController = new Vector2(0.35f, 0.85f);

        [SerializeField]
        private Handeness handeness;

        [Space]
        [SerializeField]
        private FlexFactory.FlexType initialFlexType = FlexFactory.FlexType.PinchTriggerFlex;

        private Dictionary<FlexFactory.FlexType, FlexInterface> _flexInterfaces;
        private FlexFactory.FlexType _currentFlexType;
        public FlexInterface CurrentFlexInterface
        {
            get
            {
                if (_flexInterfaces.TryGetValue(_currentFlexType, out var flex))
                {
                    return flex;
                }
                return null;
            }
        }


        private float _lastTimeApproachedGrabbable = -1.0f;
        private float _lastGrabTime = -1.0f;
        private const float GRAB_ATTEMPT_DURATION = 2.0f;
        private const float ACTUAL_GRAB_BUFFER_TIME = 1.5f;
        private Grabbable _lastGrabbableCloseBy = null;

        private OVRInput.Controller _touch;

        private Vector3 _prevPosition;
        private Quaternion _prevRotation;

        private Vector3 _velocity;
        private Vector3 _angularVelocity;


        private const float VELOCITY_DAMPING = 20f;


        protected override void Reset()
        {
            base.Reset();
            if (name.ToLower().Contains("right"))
            {
                handeness = Handeness.Right;
            }
            else
            {
                handeness = Handeness.Left;
            }
        }

        protected override void Awake()
        {
            _touch = handeness == Handeness.Right ? OVRInput.Controller.RTouch : OVRInput.Controller.LTouch;
            InitializeFlexTypes();
            base.Awake();
        }

        protected override void Grab(Grabbable closestGrabbable, Collider closestGrabbableCollider)
        {
            base.Grab(closestGrabbable, closestGrabbableCollider);

            if (GrabbedObject != null)
            {
                _prevPosition = GrabbedObject.transform.position;
                _prevRotation = GrabbedObject.transform.rotation;
            }
        }

        protected void LateUpdate()
        {
            foreach (var flexInterface in _flexInterfaces.Values)
            {
                flexInterface.Update(this.transform);
            }

            if (GrabbedObject != null)
            {
                UpdateVelocity(GrabbedObject.transform);
            }
        }

        private bool _nearGrab;
        protected override void CheckForGrabOrRelease(float prevFlex, float currentFlex)
        {
            UpdateGrabbableCloseBy();

            var currentGrabState = CurrentFlexInterface.CurrentGrabState;
            if (currentGrabState == GrabState.Failed)
            {
                GrabFail();
            }
            else if (currentGrabState == GrabState.Begin)
            {
                _nearGrab = false;
                GrabBegin();
            }
            else if (currentGrabState == GrabState.End)
            {
                GrabEnd();
            }

            if (GrabbedObject == null 
                && CurrentFlexInterface.CurrentGrabStrength > 0)
            {
                _nearGrab = true;
                NearGrab(CurrentFlexInterface.CurrentGrabStrength);
            }
            else if (_nearGrab)
            {
                _nearGrab = false;
                NearGrab(0f);
            }
        }

        private void UpdateGrabbableCloseBy()
        {
            Grabbable closestGrabbable;
            Collider closestGrabbableCollider;
            (closestGrabbable, closestGrabbableCollider) = FindClosestGrabbable();
            if (closestGrabbable != null)
            {
                _lastTimeApproachedGrabbable = Time.time;
                _lastGrabbableCloseBy = closestGrabbable;
            }
        }


        private void GrabFail()
        {
            float currentTime = Time.timeSinceLevelLoad;
            // if we tried to make a grab attempt sometime in the past.
            // but not too far into the past
            if (_lastTimeApproachedGrabbable > 0.0f 
                && currentTime < (_lastTimeApproachedGrabbable + GRAB_ATTEMPT_DURATION) 
                && currentTime > (_lastGrabTime + ACTUAL_GRAB_BUFFER_TIME) // make sure we haven't grabbed recently
                && _lastGrabbableCloseBy != null)
            {
                OnGrabAttemptFail?.Invoke(_lastGrabbableCloseBy.gameObject);
                // reset time variable so that we can't trigger multiple grab fail events
                // once hand pulls away
                _lastTimeApproachedGrabbable = -1.0f;
            }
        }


        private void UpdateVelocity(Transform relativeTo)
        {
            Vector3 instantVelocity = (relativeTo.position - _prevPosition) / Time.deltaTime;

            Quaternion deltaRotation = relativeTo.rotation * Quaternion.Inverse(_prevRotation);
            float theta = 2.0f * Mathf.Acos(Mathf.Clamp(deltaRotation.w, -1.0f, 1.0f));
            if (theta > Mathf.PI)
            {
                theta -= 2.0f * Mathf.PI;
            }
            Vector3 angularVelocity = new Vector3(deltaRotation.x, deltaRotation.y, deltaRotation.z).normalized * theta / Time.deltaTime;

            _velocity = Vector3.Lerp(instantVelocity, _velocity, Time.deltaTime * VELOCITY_DAMPING);
            _angularVelocity = Vector3.Lerp(angularVelocity, _angularVelocity, Time.deltaTime * VELOCITY_DAMPING);

            _prevPosition = relativeTo.position;
            _prevRotation = relativeTo.rotation;
        }

        public override float CurrentFlex()
        {
            if (IsUsingHands)
            {
                return CurrentFlexInterface.CurrentGrabStrength;
            }
            else
            {
                return OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, _touch);
            }
        }

        public override Vector2 GrabFlexThresold
        {
            get
            {
                return Vector2.up; //TODO: IsUsingHands ? CurrentFlexInterface. : grabThresoldController;
            }
        }

        private bool IsUsingHands => trackedHand && trackedHand.IsTracked;

        protected override (Vector3, Vector3) HandRelativeVelocity(Pose offsetPose)
        {
            return (_velocity, _angularVelocity);
        }


        #region flex engine

        void InitializeFlexTypes()
        {
            _flexInterfaces = new Dictionary<FlexFactory.FlexType, FlexInterface>();
            OVRSkeleton skeleton = trackedHand.GetComponent<OVRSkeleton>();

            var allFlexTypes = Enum.GetValues(typeof(FlexFactory.FlexType));
            foreach (FlexFactory.FlexType flexType in allFlexTypes)
            {
                var flexInterface = FlexFactory.Instance.CreateFlexInterface(
                  flexType, trackedHand, _touch, skeleton, handAnchor);

                _flexInterfaces.Add(flexType, flexInterface);
                flexInterface.Enable();

                if (flexInterface.InterfaceFlexType == initialFlexType)
                {
                    _currentFlexType = flexType;
                    CurrentFlexInterface.VisualIndicatorEnabled = true;
                }
                else
                {
                    flexInterface.VisualIndicatorEnabled = false;
                }
            }
        }

        #endregion
    }
}