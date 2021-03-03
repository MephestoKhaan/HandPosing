using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HandPosing.TrackingData;

namespace HandPosing.OVRIntegration
{
    /// <summary>
    /// Custom implementation for the Oculus plugin of the SkeletonDataProvider.
    /// It is important to note that since OVRSkeleton gets executed at -80, we want
    /// to update the data as soon as it is available (hence the -70)
    /// </summary>
    [DefaultExecutionOrder(-70)]
    public class SkeletonDataProviderOVR : SkeletonDataProvider
    {
        /// <summary>
        /// Oculus Skeleton provider for the hand.
        /// </summary>
        [SerializeField]
        private OVRSkeleton ovrSkeleton;
        [SerializeField]
        private OVRHand ovrHand;

        /// <summary>
        /// List of bone IDs in Oculus and the HandPosing data to perform the translation.
        /// </summary>
        private static readonly Dictionary<OVRSkeleton.BoneId, BoneId> OVRFingersToPosingIDs =
            new Dictionary<OVRSkeleton.BoneId, BoneId>()
            {
                {OVRSkeleton.BoneId.Hand_Thumb0 , BoneId.Hand_Thumb0},
                {OVRSkeleton.BoneId.Hand_Thumb1 , BoneId.Hand_Thumb1},
                {OVRSkeleton.BoneId.Hand_Thumb2 , BoneId.Hand_Thumb2},
                {OVRSkeleton.BoneId.Hand_Thumb3 , BoneId.Hand_Thumb3},
                {OVRSkeleton.BoneId.Hand_Index1 , BoneId.Hand_Index1},
                {OVRSkeleton.BoneId.Hand_Index2 , BoneId.Hand_Index2},
                {OVRSkeleton.BoneId.Hand_Index3 , BoneId.Hand_Index3},
                {OVRSkeleton.BoneId.Hand_Middle1 , BoneId.Hand_Middle1},
                {OVRSkeleton.BoneId.Hand_Middle2 , BoneId.Hand_Middle2},
                {OVRSkeleton.BoneId.Hand_Middle3 , BoneId.Hand_Middle3},
                {OVRSkeleton.BoneId.Hand_Ring1 , BoneId.Hand_Ring1},
                {OVRSkeleton.BoneId.Hand_Ring2 , BoneId.Hand_Ring2},
                {OVRSkeleton.BoneId.Hand_Ring3 , BoneId.Hand_Ring3},
                {OVRSkeleton.BoneId.Hand_Pinky0 , BoneId.Hand_Pinky0},
                {OVRSkeleton.BoneId.Hand_Pinky1 , BoneId.Hand_Pinky1},
                {OVRSkeleton.BoneId.Hand_Pinky2 , BoneId.Hand_Pinky2},
                {OVRSkeleton.BoneId.Hand_Pinky3 , BoneId.Hand_Pinky3}
            };

        private static readonly Dictionary<BoneId, OVRHand.HandFinger> PosingIDsToFinger =
            new Dictionary<BoneId, OVRHand.HandFinger>()
            {
                { BoneId.Hand_Thumb0, OVRHand.HandFinger.Thumb },
                { BoneId.Hand_Thumb1, OVRHand.HandFinger.Thumb},
                { BoneId.Hand_Thumb2, OVRHand.HandFinger.Thumb},
                { BoneId.Hand_Thumb3, OVRHand.HandFinger.Thumb},
                { BoneId.Hand_Index1, OVRHand.HandFinger.Index},
                { BoneId.Hand_Index2, OVRHand.HandFinger.Index},
                { BoneId.Hand_Index3, OVRHand.HandFinger.Index},
                { BoneId.Hand_Middle1, OVRHand.HandFinger.Middle},
                { BoneId.Hand_Middle2, OVRHand.HandFinger.Middle},
                { BoneId.Hand_Middle3, OVRHand.HandFinger.Middle},
                { BoneId.Hand_Ring1, OVRHand.HandFinger.Ring},
                { BoneId.Hand_Ring2, OVRHand.HandFinger.Ring},
                { BoneId.Hand_Ring3, OVRHand.HandFinger.Ring},
                { BoneId.Hand_Pinky0, OVRHand.HandFinger.Pinky},
                { BoneId.Hand_Pinky1, OVRHand.HandFinger.Pinky},
                { BoneId.Hand_Pinky2, OVRHand.HandFinger.Pinky},
                { BoneId.Hand_Pinky3, OVRHand.HandFinger.Pinky}
            };

        private static readonly List<OVRSkeleton.BoneId> OVRSkeletonFingerIds = new List<OVRSkeleton.BoneId>(OVRFingersToPosingIDs.Keys);
        private static readonly List<BoneId> FingerBoneIds = new List<BoneId>(OVRFingersToPosingIDs.Values);

        private static readonly (OVRSkeleton.BoneId, BoneId) HandBoneIDs = (OVRSkeleton.BoneId.Hand_Start, BoneId.Hand_Start);

        private BonePose[] _fingers;
        public override BonePose[] Fingers
        {
            get
            {
                return _fingers;
            }
        }

        private BonePose _hand;
        public override BonePose Hand
        {
            get
            {
                return _hand;
            }
        }

        public override bool IsTracking
        {
            get
            {
                return ovrSkeleton != null
                    && ovrSkeleton.IsInitialized
                    && ovrSkeleton.IsDataValid
                    && _fingers != null;
            }
        }

        public override bool IsHandHighConfidence()
        {
            return IsTracking
                && ovrHand.IsDataHighConfidence;
        }

        public override bool IsFingerHighConfidence(BoneId fingerId)
        {
            if (PosingIDsToFinger.TryGetValue(fingerId, out OVRHand.HandFinger id))
            {
                return ovrHand.GetFingerConfidence(id) == OVRHand.TrackingConfidence.High;
            }
            return true;
        }


        public override float? HandScale
        {
            get
            {
                return RetrieveHandScale();
            }
        }

        private float? RetrieveHandScale()
        {
            if (IsTracking)
            {
                OVRPlugin.Hand handeness = ovrSkeleton.GetSkeletonType() == OVRSkeleton.SkeletonType.HandLeft ? OVRPlugin.Hand.HandLeft : OVRPlugin.Hand.HandRight;
                OVRPlugin.HandState handState = new OVRPlugin.HandState();
                if (OVRPlugin.GetHandState(OVRPlugin.Step.Render, handeness, ref handState))
                {
                    return handState.HandScale;
                }
            }
            return null;
        }


        private void Reset()
        {
            ovrSkeleton = this.GetComponent<OVRSkeleton>();
            ovrHand = this.GetComponent<OVRHand>();
        }

        private IEnumerator Start()
        {
            while (ovrSkeleton == null
                  || !ovrSkeleton.IsInitialized)
            {
                yield return null;
            }
            _fingers = new BonePose[OVRFingersToPosingIDs.Count];
            OnInitialized?.Invoke();
        }

        private void Update()
        {
            if (_fingers != null)
            {
                UpdateBones();
                OnUpdated?.Invoke(Time.deltaTime);
            }
        }

        protected void UpdateBones()
        {
            for (int i = 0; i < OVRFingersToPosingIDs.Count; i++)
            {
                OVRBone ovrBone = ovrSkeleton.Bones[(int)OVRSkeletonFingerIds[i]];
                _fingers[i] = new BonePose()
                {
                    boneID = FingerBoneIds[i],
                    rotation = ovrBone.Transform.localRotation,
                    position = ovrBone.Transform.localPosition
                };
            }

            OVRBone ovrHand = ovrSkeleton.Bones[(int)HandBoneIDs.Item1];
            _hand = new BonePose
            {
                boneID = HandBoneIDs.Item2,
                rotation = ovrHand.Transform.rotation,
                position = ovrHand.Transform.position
            };
        }
    }
}