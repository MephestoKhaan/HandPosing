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
        [SerializeField]
        private Vector3 poseVolumeOffset = new Vector3(0.07f, -0.03f, 0.0f);
        [SerializeField]
        private bool trackLowConfidenceHands = false;
        [SerializeField]
        private bool trackLowConfidenceFingers = false;
        [SerializeField]
        private List<OVRHand.HandFinger> fingersToIgnore = new List<OVRHand.HandFinger>();
        [SerializeField]
        [Tooltip("Grab threshold, hand sphere grab")]
        private Vector2 grabThresoldHand = new Vector2(0.25f, 0.75f);


        public FlexType InterfaceFlexType
        {
            get
            {
                return FlexType.SphereGrab;
            }
        }

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
            get => CalculateGrabStrength();
        }

        public Vector2 GrabThreshold
        {
            get => grabThresoldHand;
        }

        public Vector2 FailGrabThreshold
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

        private void UpdateVolumeCenter()
        {
            OVRBone baseBone = skeleton.Bones[(int)OVRSkeleton.BoneId.Hand_Start];
            _poseVolumeCenter = baseBone.Transform.position + baseBone.Transform.TransformDirection(poseVolumeOffset);
        }

        private void CalculatePinchStrength()
        {
            bool canTrackThumb = CanTrackFinger(OVRHand.HandFinger.Thumb);
            for (int i = 0; i < FINGER_COUNT; ++i)
            {
                if (!canTrackThumb
                    || !CanTrackFinger(HAND_FINGERS[i]))
                {
                    continue;
                }

                var fingerId = HAND_FINGERS[i];
                _pinchStrength[i] = flexHand.GetFingerPinchStrength(fingerId);
            }
        }

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

        private float StorePerFingerGrabAndGetFinalValue()
        {
            // start with pose strength at first
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
    }
}