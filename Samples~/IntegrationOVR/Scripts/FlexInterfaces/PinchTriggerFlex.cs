using System;
using UnityEngine;

namespace HandPosing.OVRIntegration.GrabEngine
{
    /// <summary>
    /// Stock flex detector used in snap-to-pose.
    /// </summary>
    public class PinchTriggerFlex : MonoBehaviour, FlexInterface
    {
        [SerializeField]
        private OVRHand flexhand;
        [SerializeField]
        private OVRInput.Controller controller;

        [Space]
        [SerializeField]
        [Tooltip("Grab threshold, left hand controller")]
        private Vector2 grabThresoldController = new Vector2(0.35f, 0.55f);
        [SerializeField]
        [Tooltip("Grab threshold, left hand pinch")]
        private Vector2 grabThresoldHand = new Vector2(0.35f, 0.95f);

        private const float ALMOST_PINCH_LOWER_PERCENT = 1.2f;
        private const float ALMOST_PINCH_UPPER_PERCENT = 0.75f;

        public FlexType InterfaceFlexType => FlexType.PinchTriggerFlex;

        public float GrabStrength
        {
            get
            {
                if (flexhand
                    && flexhand.IsTracked)
                {
                      return Math.Max(
                          flexhand.GetFingerPinchStrength(OVRHand.HandFinger.Index),
                          flexhand.GetFingerPinchStrength(OVRHand.HandFinger.Middle));
                }
                else
                {
                    return OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger,controller);
                }
            }
        }

        public Vector2 GrabThresold
        {
            get
            {
                if (flexhand
                    && flexhand.IsTracked)
                {
                    return grabThresoldHand;
                }
                else
                {
                    return grabThresoldController;
                }
            }
        }

        public Vector2 FailGrabThresold
        {
            get
            {
                Vector2 failThresold = GrabThresold;
                failThresold.x *= ALMOST_PINCH_LOWER_PERCENT;
                failThresold.y *= ALMOST_PINCH_UPPER_PERCENT;
                return failThresold;
            }
        }
    }
}
