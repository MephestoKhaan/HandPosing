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

        public Vector2 GrabThreshold
        {
            get
            {
                if (_lastWasSphereFlex)
                {
                    return sphereFlex.GrabThreshold;
                }
                else
                {
                    return pinchFlex.GrabThreshold;
                }
            }
        }

        public Vector2 FailGrabThreshold
        {
            get
            {
                if (_lastWasSphereFlex)
                {
                    return sphereFlex.FailGrabThreshold;
                }
                else
                {
                    return pinchFlex.FailGrabThreshold;
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

        private void Reset()
        {
            sphereFlex = this.GetComponent<SphereGrabFlex>();
            pinchFlex = this.GetComponent<PinchTriggerFlex>();
        }

        public float? CalculateGrabStrength()
        {
            _lastWasSphereFlex = false;

            float pinchStrength = RemapedFlex(pinchFlex, out float rawPinchStrength) ?? -1f;
            if (pinchStrength == 1f)
            {
                return 1f;
            }

            float sphereStrength = RemapedFlex(sphereFlex, out float rawSphereStrength) ?? -1f;
            if (sphereStrength > pinchStrength)
            {
                _lastWasSphereFlex = true;
                return rawSphereStrength;
            }

            if (pinchStrength == -1f)
            {
                return null;
            }

            return rawPinchStrength;
        }

        private static float? RemapedFlex(FlexInterface flex, out float rawStrenght)
        {
            float? strenght = flex.GrabStrength;
            rawStrenght = strenght ?? -1f;
            if (!strenght.HasValue)
            {
                return null;
            }
            Vector2 range = flex.GrabThreshold;
            return RemapClamped(strenght.Value, range.x, range.y, 0f, 1f);
        }

        private static float RemapClamped(float value, float low1, float high1, float low2 = 0f, float high2 = 1f)
        {
            value = low1 < high1 ? Mathf.Clamp(value, low1, high1) : Mathf.Clamp(value, high1, low1);
            return low2 + (value - low1) * (high2 - low2) / (high1 - low1);
        }
    }
}