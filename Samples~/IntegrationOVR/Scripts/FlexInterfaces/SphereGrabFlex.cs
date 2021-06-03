// Copyright(c) Facebook Technologies, LLC and its affiliates. All rights reserved.

using System.Collections.Generic;
using UnityEngine;

namespace HandPosing.OVRIntegration.GrabEngine
{
    /// <summary>
    /// Sphere grab detector.
    /// They use poses (in this case hand state's raw pose) to determine if
    /// hand fingers are curled up into a ball. In this case we use the capsules to
    /// do determine the finger tips as well as the volume that curl into.
    /// </summary>
    public class SphereGrabFlex : MonoBehaviour, FlexInterface
    {
        [SerializeField]
        private OVRHand flexHand = null;
        [SerializeField]
        private OVRSkeleton skeleton = null;

        [Space]
        [SerializeField]
        [Range(0.0f, 0.05f)]
        private float fingerTipRadius = 0.01f;
        [SerializeField]
        [Range(0.0f, 0.1f)]
        private float poseVolumeRadius = 0.07f;
        [Space]
        [SerializeField]
        [Tooltip("Set this to automatically calculate the Pose Volume Offset")]
        private Transform optionalCenterPoint;
        [SerializeField]
        [Tooltip("This will differ based on handedness. The right can be (0.07f,-0.03f, 0.0f), while the left can be the inverse (-0.07f, 0.03f, 0.0f).")]
        private Vector3 poseVolumeOffset = new Vector3(0.07f, -0.03f, 0.0f);
        [Space]
        [SerializeField]
        private bool trackLowConfidenceHands = false;
        [SerializeField]
        private bool trackLowConfidenceFingers = false;
        [SerializeField]
        private List<OVRHand.HandFinger> fingersToIgnore = new List<OVRHand.HandFinger>();
        [SerializeField]
        [Tooltip("Grab threshold, hand sphere grab")]
        private Vector2 grabThresoldHand = new Vector2(0.25f, 0.75f);

        private float? _lastGrabStrength;

        private float[] _fingerPoseStrength = new float[FINGER_COUNT];
        private float[] _fingerGrabStrength = new float[FINGER_COUNT];
        private float[] _pinchStrength = new float[FINGER_COUNT];
        private Vector3[] _fingerTipCenter = new Vector3[FINGER_COUNT];
        private Vector3 _poseVolumeCenter = Vector3.zero;
        private bool[] _isFingerIgnored;

        private const float ALMOST_GRAB_LOWER_PERCENT = 1.7f;
        private const float ALMOST_GRAB_UPPER_PERCENT = 0.9f;
        private const float ALMOST_GRAB_RELEASE_PERCENT = 1.4f;

        private const int FINGER_COUNT = 5;

        private static readonly OVRHand.HandFinger[] HAND_FINGERS = new OVRHand.HandFinger[FINGER_COUNT]
        {
            OVRHand.HandFinger.Thumb,
            OVRHand.HandFinger.Index,
            OVRHand.HandFinger.Middle,
            OVRHand.HandFinger.Ring,
            OVRHand.HandFinger.Pinky
        };
        private static readonly OVRSkeleton.BoneId[] FINGER_TIPS = new OVRSkeleton.BoneId[FINGER_COUNT]
        {
            OVRSkeleton.BoneId.Hand_ThumbTip,
            OVRSkeleton.BoneId.Hand_IndexTip,
            OVRSkeleton.BoneId.Hand_MiddleTip,
            OVRSkeleton.BoneId.Hand_RingTip,
            OVRSkeleton.BoneId.Hand_PinkyTip
        };


        public bool IsValid => flexHand && flexHand.IsDataValid;

        public float? GrabStrength
        {
            get => CalculateGrabStrength();
        }

        public Vector2 GrabThreshold
        {
            get => grabThresoldHand;
        }

        public Vector2 GrabAttemptThreshold
        {
            get => GrabThreshold * new Vector2(ALMOST_GRAB_LOWER_PERCENT, ALMOST_GRAB_UPPER_PERCENT);
        }

        public float AlmostGrabRelease
        {
            get => GrabThreshold.x * ALMOST_GRAB_RELEASE_PERCENT;
        }

        private bool IsFinderIgnored(int fingerIndex)
        {
            if (_isFingerIgnored == null)
            {
                _isFingerIgnored = new bool[FINGER_COUNT];
                for (int i = 0; i < FINGER_COUNT; ++i)
                {
                    _isFingerIgnored[i] = fingersToIgnore.Contains(HAND_FINGERS[i]);
                }
            }

            return _isFingerIgnored[fingerIndex];
        }

        private float? CalculateGrabStrength()
        {
            if (!IsValid)
            {
                return null;
            }

            if (CanTrackHand())
            {
                UpdateFingerTips();
                UpdateVolumeCenter();

                CalculatePinchStrength();
                CalculatePoseStrength();
                _lastGrabStrength = StorePerFingerGrabAndGetFinalValue();
            }

            return _lastGrabStrength;
        }

        private bool CanTrackHand()
        {
            return flexHand.IsDataHighConfidence || trackLowConfidenceHands;
        }

        /// <summary>
        /// Updates the position of the finger tips, for later comparison with the Hand Centre
        /// </summary>
        private void UpdateFingerTips()
        {
            for (int i = 0; i < FINGER_COUNT; ++i)
            {
                if (!CanTrackFinger(HAND_FINGERS[i]))
                {
                    continue;
                }

                var tipId = FINGER_TIPS[i];
                OVRBone fingerTip = skeleton.Bones[(int)tipId];
                _fingerTipCenter[i] = fingerTip.Transform.position;
            }
        }

        private bool CanTrackFinger(OVRHand.HandFinger finger)
        {
            return (flexHand.GetFingerConfidence(finger) == OVRHand.TrackingConfidence.High || trackLowConfidenceFingers);
        }

        /// <summary>
        /// Updates the position of the Hand Centre. It is important to visualise that the offset is correct when editing it.
        /// </summary>
        private void UpdateVolumeCenter()
        {
            OVRBone baseBone = skeleton.Bones[(int)OVRSkeleton.BoneId.Hand_Start];
            _poseVolumeCenter = baseBone.Transform.position + baseBone.Transform.TransformDirection(poseVolumeOffset);
        }

        /// <summary>
        /// To calculate the pinch strength we must check not only the confidence of each finger but also the thumb.
        /// After that we ony update the values if the tracking was good enough (controllable in the inspector)
        /// </summary>
        private void CalculatePinchStrength()
        {
            bool canTrackThumb = CanTrackFinger(OVRHand.HandFinger.Thumb);
            if(!canTrackThumb)
            {
                return;
            }
            for (int i = 0; i < FINGER_COUNT; ++i)
            {
                if (CanTrackFinger(HAND_FINGERS[i]))
                {
                    var fingerId = HAND_FINGERS[i];
                    _pinchStrength[i] = flexHand.GetFingerPinchStrength(fingerId);
                }
            }
        }

        /// <summary>
        /// To calculate the sphere grab strenght we check that the finger tips are close enough to the palm centre with some hysteresis specified vy the radious.
        /// </summary>
        private void CalculatePoseStrength()
        {
            float outsidePoseVolumeRadius = poseVolumeRadius + fingerTipRadius;
            float insidePoseVolumeRadius = poseVolumeRadius - fingerTipRadius;
            float sqrOutsidePoseVolume = outsidePoseVolumeRadius * outsidePoseVolumeRadius;
            float sqrInsidePoseVolume = insidePoseVolumeRadius * insidePoseVolumeRadius;

            for (int i = 0; i < FINGER_COUNT; ++i)
            {
                float sqrDist = (_poseVolumeCenter - _fingerTipCenter[i]).sqrMagnitude;
                if (sqrDist >= sqrOutsidePoseVolume)
                {
                    _fingerPoseStrength[i] = 0.0f;
                }
                else if (sqrDist <= sqrInsidePoseVolume)
                {
                    _fingerPoseStrength[i] = 1.0f;
                }
                else
                {
                    // interpolate in-between; 1 inside, 0 outside
                    float distance = Mathf.Sqrt(sqrDist);
                    _fingerPoseStrength[i] = 1.0f - Mathf.Clamp01((distance - insidePoseVolumeRadius) / (2.0f * fingerTipRadius));
                }
            }
        }

        /// <summary>
        /// Once all positions and distances are calculated, proceeds to update the final value.
        /// For each finger the grab value is the max between the pinch and the palm distance
        /// Then, taking in count that fingers can be ignored, finds the minimal value (all fingers must be grabbing).
        /// It also populates the grab strength collection with all the relevant values.
        /// </summary>
        /// <returns>The safest grab value for the hand</returns>
        private float StorePerFingerGrabAndGetFinalValue()
        {
            for (int i = 0; i < _fingerGrabStrength.Length; ++i)
            {
                _fingerGrabStrength[i] = _fingerPoseStrength[i];
            }

            // Calculate finger grab strength while taking pinch into account
            float? minGrabStrength = null;
            for (int i = 0; i < FINGER_COUNT; ++i)
            {
                var grabStrength = Mathf.Max(_pinchStrength[i], _fingerGrabStrength[i]);
                _fingerGrabStrength[i] = grabStrength;

                if (IsFinderIgnored(i))
                {
                    continue;
                }
                if (!minGrabStrength.HasValue 
                    || minGrabStrength > grabStrength)
                {
                    minGrabStrength = grabStrength;
                }
            }

            return minGrabStrength ?? 0f;
        }

        private void OnValidate()
        {
            if(optionalCenterPoint != null)
            {
                SetVolumeOffset(optionalCenterPoint);
            }
        }

        public void SetVolumeOffset(Transform centre)
        {
            if(centre != null)
            {
                optionalCenterPoint = centre;
                poseVolumeOffset = this.transform.InverseTransformPoint(optionalCenterPoint.position);
            }
        }
    }
}