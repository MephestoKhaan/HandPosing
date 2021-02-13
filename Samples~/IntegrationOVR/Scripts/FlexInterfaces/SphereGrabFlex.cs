// Copyright(c) Facebook Technologies, LLC and its affiliates. All rights reserved.

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
        private OVRInput.Controller _controller;
        [SerializeField]
        private OVRHand _ovrHand = null;
        [SerializeField]
        private OVRSkeleton _skeleton = null;

        [Space]
        public OVRHand.HandFinger[] DisabledFingers;
        [SerializeField]
        [Range(0.0f, 0.05f)]
        private float _fingerTipRadius = 0.01f;
        [SerializeField]
        [Range(0.0f, 0.1f)]
        private float _poseVolumeRadius = 0.055f;
        [SerializeField]
        private Vector3 _poseVolumeOffset = new Vector3(0.0f, 0.04f, 0.0f);
        [SerializeField]
        private bool _trackLowConfidenceHands = false;
        [SerializeField]
        private bool _trackLowConfidenceFingers = false;
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

        private float[] _fingerPoseStrength = new float[FINGER_COUNT];
        private float[] _fingerGrabStrength = new float[FINGER_COUNT];
        private float[] _pinchStrength = new float[FINGER_COUNT];
        private Vector3[] _fingerTipCenter = new Vector3[FINGER_COUNT];
        private Vector3 _poseVolumeCenter = Vector3.zero;

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


        public float GrabStrength
        {
            get => CalculateGrabStrength();
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

        public float CalculateGrabStrength()
        {
            UpdateFingerTips();
            UpdateVolumeCenter();

            if (CanTrackHand())
            {
                CalculatePinchStrength();
                CalculatePoseStrength();
            }

            return StorePerFingerGrabAndGetFinalValue();
        }

        private bool CanTrackHand()
        {
            if (_ovrHand == null
                || !_ovrHand.IsDataValid
                || (!_ovrHand.IsDataHighConfidence && !_trackLowConfidenceHands))
            {
                return false;
            }
            return true;
        }

        private bool CanTrackFinger(int fingerIndex)
        {
            OVRHand.HandFinger finger = HAND_FINGERS[fingerIndex];

            if (_ovrHand != null
                || !_ovrHand.IsDataValid
                || (!_trackLowConfidenceFingers && _ovrHand.GetFingerConfidence(finger) != OVRHand.TrackingConfidence.High))
            {
                return false;
            }
            return true;
        }

        private void UpdateFingerTips()
        {
            for (int i = 0; i < FINGER_COUNT; ++i)
            {
                if (!CanTrackFinger(i))
                {
                    continue;
                }

                var tipId = FINGER_TIPS[i];
                OVRBone fingerTip = _skeleton.Bones[(int)tipId];
                _fingerTipCenter[i] = fingerTip.Transform.position;
            }

        }

        private void UpdateVolumeCenter()
        {
            OVRBone baseBone = _skeleton.Bones[(int)OVRSkeleton.BoneId.Hand_Start];
            _poseVolumeCenter = baseBone.Transform.position + baseBone.Transform.TransformDirection(_poseVolumeOffset);
        }

        private void CalculatePinchStrength()
        {
            for (int i = 0; i <= FINGER_COUNT; ++i)
            {
                if (!CanTrackFinger(i))
                {
                    continue;
                }

                var fingerId = HAND_FINGERS[i];
                _pinchStrength[i] = _ovrHand.GetFingerPinchStrength(fingerId);
            }
        }

        private void CalculatePoseStrength()
        {
            float outsidePoseVolumeRadius = _poseVolumeRadius + _fingerTipRadius;
            float insidePoseVolumeRadius = _poseVolumeRadius - _fingerTipRadius;
            float sqrOutsidePoseVolume = outsidePoseVolumeRadius * outsidePoseVolumeRadius;
            float sqrInsidePoseVolume = insidePoseVolumeRadius * insidePoseVolumeRadius;

            for (int i = 0; i <= FINGER_COUNT; ++i)
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
                    _fingerPoseStrength[i] = 1.0f - Mathf.Clamp01((distance - insidePoseVolumeRadius) / (2.0f * _fingerTipRadius));
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
            float minGrabStrength = float.MaxValue;
            for (int i = 0; i < FINGER_COUNT; ++i)
            {
                var grabStrength = Mathf.Max(_pinchStrength[i], _fingerGrabStrength[i]);
                if (minGrabStrength > grabStrength)
                {
                    minGrabStrength = grabStrength;
                }
                _fingerGrabStrength[i] = grabStrength;
            }

            return minGrabStrength;
        }
    }
}