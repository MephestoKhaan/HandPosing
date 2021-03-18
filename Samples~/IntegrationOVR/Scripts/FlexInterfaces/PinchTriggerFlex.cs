using UnityEngine;

namespace HandPosing.OVRIntegration.GrabEngine
{
    /// <summary>
    /// This flex interface compares if the user is pinching with the Index or the Middle Finger.
    /// It can track the pinches in high and low confidence if specified, and tipically returns a fairly safe value since 
    /// it relies on OVR methods for calculating the pinching.
    /// </summary>
    public class PinchTriggerFlex : MonoBehaviour, FlexInterface
    {
        [SerializeField]
        private OVRHand flexHand;
        [SerializeField]
        private bool trackLowConfidenceFingers = false;

        [Space]
        [SerializeField]
        [Tooltip("Grab threshold, hand pinch")]
        private Vector2 grabThresold = new Vector2(0.01f, 0.9f);

        private const float ALMOST_PINCH_LOWER_PERCENT = 1.2f;
        private const float ALMOST_PINCH_UPPER_PERCENT = 0.75f;
        private const int FINGER_COUNT = 2;

        private float[] _pinchStrength = new float[FINGER_COUNT];
        private static readonly OVRHand.HandFinger[] PINCHING_FINGERS = new OVRHand.HandFinger[FINGER_COUNT]
        {
            OVRHand.HandFinger.Index,
            OVRHand.HandFinger.Middle
        };

        public bool IsValid
        {
            get
            {
                return flexHand
                    && flexHand.IsDataValid;
            }
        }

        public float? GrabStrength
        {
            get
            {
                if (IsValid)
                {
                    return CalculateStrength();
                }
                return null;
            }
        }

        public Vector2 GrabThreshold
        {
            get => grabThresold;
        }

        public Vector2 GrabAttemptThreshold
        {
            get => GrabThreshold * new Vector2(ALMOST_PINCH_LOWER_PERCENT, ALMOST_PINCH_UPPER_PERCENT);
        }

        public float AlmostGrabRelease
        {
            get => GrabThreshold.x;
        }

        /// <summary>
        /// In order to calculate the strength first we check if the hand and the thumb are being tracked.
        /// Then we find the max value between the pinching fingers and that will be the one returned.
        /// </summary>
        /// <returns>A normalised value indicating the max pinch of the middle and index fingers</returns>
        private float CalculateStrength()
        {
            float maxPinch = 0f;
            bool isHandTracked = IsHandTracked();
            bool isThumbTracked = isHandTracked && IsFingerTracked(OVRHand.HandFinger.Thumb);

            for (int i = 0; i < FINGER_COUNT; i++)
            {
                float rawPinch = flexHand.GetFingerPinchStrength(PINCHING_FINGERS[i]); 
                if (isThumbTracked
                    && IsFingerTracked(PINCHING_FINGERS[i]))
                {
                    _pinchStrength[i] = rawPinch;
                }
                else
                {
                    _pinchStrength[i] = Mathf.Max(_pinchStrength[i], rawPinch);
                }

                maxPinch = Mathf.Max(maxPinch, _pinchStrength[i]);
            }
            return maxPinch;
        }

        private bool IsHandTracked()
        {
            return flexHand.IsTracked && (flexHand.HandConfidence == OVRHand.TrackingConfidence.High || trackLowConfidenceFingers);
        }

        private bool IsFingerTracked(OVRHand.HandFinger finger)
        {
            return flexHand.GetFingerConfidence(finger) == OVRHand.TrackingConfidence.High || trackLowConfidenceFingers;
        }
    }
}
