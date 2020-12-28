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
        [SerializeField]
        private Transform trackingSpace;
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
        private OVRInput.Controller hand;

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
            hand = handeness == Handeness.Right ? OVRInput.Controller.RHand : OVRInput.Controller.LHand;
            base.Awake();
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
            OVRInput.Controller controller = IsUsingHands ? hand : touch;

            Vector3 linearVelocity = this.trackingSpace.rotation * OVRInput.GetLocalControllerVelocity(controller);
            Vector3 angularVelocity = this.trackingSpace.rotation * OVRInput.GetLocalControllerAngularVelocity(controller);
            return (linearVelocity, angularVelocity);
        }
    }
}