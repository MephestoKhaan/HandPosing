// Copyright(c) Facebook Technologies, LLC and its affiliates. All rights reserved.

using UnityEngine;

namespace HandPosing.OVRIntegration.GrabEngine
{
    public class SphereGrabPinchFlex : MonoBehaviour, FlexInterface
    {
        [Space]
        [SerializeField]
        [Tooltip("Grab threshold, hand sphere grab")]
        private Vector2 grabThresoldHand = new Vector2(0.35f, 0.95f);

        [SerializeField]
        private SphereGrabFlex sphereFlex;
        [SerializeField]
        private PinchTriggerFlex pinchFlex;

        private const float ALMOST_GRAB_LOWER_PERCENT = 1.2f;
        private const float ALMOST_GRAB_UPPER_PERCENT = 0.75f;
        private const float ALMOST_GRAB_RELEASE_PERCENT = 1f;

        public FlexType InterfaceFlexType
        {
            get
            {
                return FlexType.SpherePinchGrab;
            }
        }

        public bool IsValid
        {
            get
            {
                return sphereFlex.IsValid
                    || pinchFlex.IsValid;
            }
        }

        public float? GrabStrength
        {
            get
            {
                return CalculateGrabStrength();
            }
        }

        public Vector2 GrabThresold
        {
            get => grabThresoldHand;
        }

        public Vector2 FailGrabThresold
        {
            get
            {
                Vector2 failThresold = GrabThresold;
                failThresold.x *= ALMOST_GRAB_LOWER_PERCENT;
                failThresold.y *= ALMOST_GRAB_UPPER_PERCENT;
                return failThresold;
            }
        }

        public float AlmostGrabRelease
        {
            get => GrabThresold.x * ALMOST_GRAB_RELEASE_PERCENT;
        }

        public float? CalculateGrabStrength()
        {
            float pinchStrenght = pinchFlex.GrabStrength ?? -1f;
            if (pinchStrenght == 1f)
            {
                return 1f;
            }
            float sphereStrenght = sphereFlex.GrabStrength ?? -1f;
            float grabStrenght = Mathf.Max(pinchStrenght, sphereStrenght);

            if(grabStrenght == -1f)
            {
                return null;
            }

            return grabStrenght;
        }
    }
}