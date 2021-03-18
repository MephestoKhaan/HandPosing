// Copyright(c) Facebook Technologies, LLC and its affiliates. All rights reserved.

using UnityEngine;

namespace HandPosing.OVRIntegration.GrabEngine
{
    /// <summary>
    /// This Flex interface serves as the Union between SphereGrabFlex and PinchTriggerFlex.
    /// It will return a grab if any of them is in their own respective valid ranges, but also
    /// populate its internal state to know if the current used value was from the sphereFlex or the pinchFlex.
    /// That way the output value can be check against the right threshold ranges in the current frame.
    /// </summary>
    public class SphereGrabPinchFlex : MonoBehaviour, FlexInterface
    {
        [SerializeField]
        private SphereGrabFlex sphereFlex;
        [SerializeField]
        private PinchTriggerFlex pinchFlex;

        private bool _lastWasSphereFlex;

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

        public Vector2 GrabAttemptThreshold
        {
            get
            {
                if (_lastWasSphereFlex)
                {
                    return sphereFlex.GrabAttemptThreshold;
                }
                else
                {
                    return pinchFlex.GrabAttemptThreshold;
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

        /// <summary>
        /// Calculates the stronger grab between the pinch and sphere flexes.
        /// In order to compare the values it remaps them using their grab strenghts.
        /// It also stores which was the best flex interface on this frame so the relevant thresholds can be compared after this method.
        /// </summary>
        /// <returns>The strongest grab value</returns>
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

        /// <summary>
        /// Remaps a Flex value from a FlexInterface to the 0-1 range considering the respective GrabThreshold. 
        /// This is relevant so we can compare which one of different FlexInterfaces is grabbing strongher.
        /// </summary>
        /// <param name="flex">The FlexInterface to extract and remap the grabStrength from</param>
        /// <param name="rawStrenght">Outputs the unmapped grabStrength of the flex interface</param>
        /// <returns>A normalised value indicating the local strength for the grab</returns>
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