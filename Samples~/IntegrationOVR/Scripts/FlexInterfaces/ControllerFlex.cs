using UnityEngine;

namespace HandPosing.OVRIntegration.GrabEngine
{
    /// <summary>
    /// This Flex Interface is used to detect grabbing when using an Oculus Controller
    /// </summary>
    public class ControllerFlex : MonoBehaviour, FlexInterface
    {
        [SerializeField]
        private OVRInput.Controller controller;

        [SerializeField]
        [Tooltip("Grab threshold, hand controller")]
        private Vector2 grabThresold = new Vector2(0.35f, 0.95f);

        private const float ALMOST_PINCH_LOWER_PERCENT = 1.2f;
        private const float ALMOST_PINCH_UPPER_PERCENT = 0.75f;

        public bool IsValid => true;

        public float? GrabStrength
        {
            get
            {
                return OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, controller);
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
    }
}