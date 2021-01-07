using HandPosing.Interaction;
using System;
using UnityEngine;

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
        /// <summary>
        /// Release (X) and Grab (Y) values for the controller trigger.
        /// </summary>
        [SerializeField]
        [Tooltip("Release (X) and Grab (Y) values for the controller trigger.")]
        private Vector2 grabThresoldController = new Vector2(0.35f, 0.85f);
        /// <summary>
        /// Release (X) and Grab (Y) values for the pinching gesture.
        /// </summary>
        [SerializeField]
        [Tooltip("Release (X) and Grab (Y) values for the pinching gesture.")]
        private Vector2 grabThresoldHand = new Vector2(0f,0.95f);

        [SerializeField]
        private Handeness handeness;

        private OVRInput.Controller touch;

        private Vector3 _prevPosition;
        private Quaternion _prevRotation;

        private Vector3 _velocity;
        private Vector3 _angularVelocity;

        private const float VELOCITY_DAMPING = 20f;


        protected override void Reset()
        {
            base.Reset();
            if(name.ToLower().Contains("right"))
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
            touch = handeness == Handeness.Right ? OVRInput.Controller.RTouch : OVRInput.Controller.LTouch;
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
            if (GrabbedObject != null)
            {
                Vector3 instantVelocity = (GrabbedObject.transform.position - _prevPosition) / Time.deltaTime;

                Quaternion deltaRotation = GrabbedObject.transform.rotation * Quaternion.Inverse(_prevRotation);
                float theta = 2.0f * Mathf.Acos(Mathf.Clamp(deltaRotation.w, -1.0f, 1.0f));
                if (theta > Mathf.PI)
                {
                    theta -= 2.0f * Mathf.PI;
                }
                Vector3 angularVelocity = new Vector3(deltaRotation.x, deltaRotation.y, deltaRotation.z).normalized * theta / Time.deltaTime;

                _velocity = Vector3.Lerp(instantVelocity, _velocity, Time.deltaTime * VELOCITY_DAMPING);
                _angularVelocity = Vector3.Lerp(angularVelocity, _angularVelocity, Time.deltaTime * VELOCITY_DAMPING);

                _prevPosition = GrabbedObject.transform.position;
                _prevRotation = GrabbedObject.transform.rotation;
            }
        }
        public override float CurrentFlex()
        {
            if (IsUsingHands)
            {
                return Math.Max(trackedHand.GetFingerPinchStrength(OVRHand.HandFinger.Index),
                     trackedHand.GetFingerPinchStrength(OVRHand.HandFinger.Middle));
            }
            else
            {
                return OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, touch);
            }
        }

        public override Vector2 GrabFlexThresold
        {
            get
            {
                return IsUsingHands ? grabThresoldHand : grabThresoldController;
            }
        }

        private bool IsUsingHands => trackedHand && trackedHand.IsTracked;

        protected override (Vector3, Vector3) HandRelativeVelocity(Pose offsetPose)
        {
            return (_velocity, _angularVelocity);
        }
    }
}