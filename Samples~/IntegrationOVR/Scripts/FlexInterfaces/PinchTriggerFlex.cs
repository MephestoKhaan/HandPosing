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
        private OVRHand flexHand;

        [Space]
        [SerializeField]
        [Tooltip("Grab threshold, hand pinch")]
        private Vector2 grabThresold = new Vector2(0.35f, 0.95f);

        private const float ALMOST_PINCH_LOWER_PERCENT = 1.2f;
        private const float ALMOST_PINCH_UPPER_PERCENT = 0.75f;

        public FlexType InterfaceFlexType => FlexType.PinchTriggerFlex;

        public bool IsValid
        {
            get
            {
                return flexHand
                    && flexHand.IsTracked;
            }
        }

        public float? GrabStrength
        {
            get
            {
                if (IsValid)
                {
                      return Math.Max(
                          flexHand.GetFingerPinchStrength(OVRHand.HandFinger.Index),
                          flexHand.GetFingerPinchStrength(OVRHand.HandFinger.Middle));
                }
                return null;
            }
        }

        public Vector2 GrabThresold
        {
            get => grabThresold;
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

        public float AlmostGrabRelease
        {
            get => GrabThresold.x;
        }
    }
}
