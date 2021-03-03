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

        private bool _lastWasSphereFlex;

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
                if (_lastWasSphereFlex)
                {
                    return sphereFlex.GrabThresold;
                }
                else
                {
                    return pinchFlex.GrabThresold;
                }
            }
        }

        public Vector2 FailGrabThresold
        {
            get
            {
                if (_lastWasSphereFlex)
                {
                    return sphereFlex.FailGrabThresold;
                }
                else
                {
                    return pinchFlex.FailGrabThresold;
                }
            }
        }

        public float AlmostGrabRelease
        {
            get
            {
                if (_lastWasSphereFlex)
                {
                    return sphereFlex.AlmostGrabRelease;
                }
                else
                {
                    return pinchFlex.AlmostGrabRelease;
                }
            }
        }


        public float? CalculateGrabStrength()
        {
            _lastWasSphereFlex = false;
            float pinchStrength = pinchFlex.GrabStrength ?? -1f;
            if (pinchStrength == 1f)
            {
                return 1f;
            }

            float sphereStrength = sphereFlex.GrabStrength ?? -1f;
            if (sphereStrength > pinchStrength)
            {
                _lastWasSphereFlex = true;
                return sphereStrength;
            }

            if (pinchStrength == -1f)
            {
                return null;
            }

            return pinchStrength;
        }
    }
}