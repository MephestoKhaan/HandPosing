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
        [SerializeField]
        private bool trackLowConfidenceFingers = false;

        [Space]
        [SerializeField]
        [Tooltip("Grab threshold, hand pinch")]
        private Vector2 grabThresold = new Vector2(0.35f, 0.95f);


        private const float ALMOST_PINCH_LOWER_PERCENT = 1.2f;
        private const float ALMOST_PINCH_UPPER_PERCENT = 0.75f;

        private const int FINGER_COUNT = 2;
        private float[] _pinchStrength = new float[FINGER_COUNT];
        private static readonly OVRHand.HandFinger[] PINCHING_FINGERS = new OVRHand.HandFinger[FINGER_COUNT]
        {
            OVRHand.HandFinger.Index,
            OVRHand.HandFinger.Middle
        };


        public FlexType InterfaceFlexType => FlexType.PinchTriggerFlex;


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

        private float CalculateStrength()
        {
            float maxPinch = 0f;
            for(int i = 0; i < FINGER_COUNT; i++)
            {
                float rawPinch = flexHand.GetFingerPinchStrength(PINCHING_FINGERS[i]); 
                if (CanTrackFinger(i))
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

        private bool CanTrackFinger(int fingerIndex)
        {
            OVRHand.HandFinger finger = PINCHING_FINGERS[fingerIndex];


            if (flexHand == null
                || !flexHand.IsTracked
                || (flexHand.HandConfidence != OVRHand.TrackingConfidence.High && !trackLowConfidenceFingers)
                || (flexHand.GetFingerConfidence(finger) != OVRHand.TrackingConfidence.High && !trackLowConfidenceFingers)
                || (flexHand.GetFingerConfidence(OVRHand.HandFinger.Thumb) != OVRHand.TrackingConfidence.High && !trackLowConfidenceFingers))
            {
                return false;
            }
            return true;
        }
    }
}
