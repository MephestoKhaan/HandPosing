// Copyright(c) Facebook Technologies, LLC and its affiliates. All rights reserved.

using UnityEngine;

namespace HandPosing.OVRIntegration.GrabEngine
{
    public class SphereGrabPinchFlex : MonoBehaviour, FlexInterface
    {
        [SerializeField]
        private SphereGrabFlex sphereFlex;
        [SerializeField]
        private PinchTriggerFlex pinchFlex;

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
            get
            {
                return new Vector2(
                    Mathf.Min(sphereFlex.GrabThresold.x, pinchFlex.GrabThresold.x),
                    Mathf.Max(sphereFlex.GrabThresold.y, pinchFlex.GrabThresold.y));
            }
        }

        public Vector2 FailGrabThresold
        {
            get
            {
                return new Vector2(
                    Mathf.Min(sphereFlex.FailGrabThresold.x, pinchFlex.FailGrabThresold.x),
                    Mathf.Max(sphereFlex.FailGrabThresold.y, pinchFlex.FailGrabThresold.y));
            }
        }

        public float AlmostGrabRelease
        {
            get
            {
                return Mathf.Min(sphereFlex.AlmostGrabRelease, pinchFlex.AlmostGrabRelease);
            }
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