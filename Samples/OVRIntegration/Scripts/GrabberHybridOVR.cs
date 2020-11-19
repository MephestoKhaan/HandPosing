using HandPosing.Interaction;
using System;
using UnityEngine;

namespace HandPosing.OVRIntegration
{
    public class GrabberHybridOVR : BaseGrabber
    {
        [Header("OVR dependencies")]
        [SerializeField]
        private OVRHand trackedHand;
        [SerializeField]
        private Vector2 grabThresoldController = new Vector2(0.35f, 0.85f);
        [SerializeField]
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

            Pose localPose = new Pose(OVRInput.GetLocalControllerPosition(controller), OVRInput.GetLocalControllerRotation(controller));
            localPose = PoseUtils.Multiply(localPose, offsetPose);

            Pose trackingSpace = PoseUtils.Multiply(transform.GetPose(), localPose.Inverse());
            Vector3 linearVelocity = trackingSpace.rotation * OVRInput.GetLocalControllerVelocity(controller);
            Vector3 angularVelocity = trackingSpace.rotation * OVRInput.GetLocalControllerAngularVelocity(controller);
            return (linearVelocity, angularVelocity);
        }
    }
}