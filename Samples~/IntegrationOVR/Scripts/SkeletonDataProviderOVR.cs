using System.Collections.Generic;
using UnityEngine;

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

        /// <summary>
        /// List of bone IDs in Oculus and the HandPosing data to perform the translation.
        /// </summary>
        private static readonly Dictionary<OVRSkeleton.BoneId, BoneId> OVRToPosingIDs =
            new Dictionary<OVRSkeleton.BoneId, BoneId>()
            {
                {OVRSkeleton.BoneId.Invalid , BoneId.Invalid},
                {OVRSkeleton.BoneId.Hand_Start , BoneId.Hand_Start},
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


        private List<HandBone> _bones;
        public override List<HandBone> Bones
        {
            get
            {
                return _bones;
            }
        }

        public override bool IsTracking
        {
            get
            {
                return ovrSkeleton != null
                    && ovrSkeleton.IsDataValid
                    && ovrSkeleton.IsInitialized
                    && _bones != null;
            }
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

        private bool CanInitialise
            => ovrSkeleton != null
                  && ovrSkeleton.IsInitialized
                  && _bones == null;

        private void Reset()
        {
            ovrSkeleton = this.GetComponent<OVRSkeleton>();
        }

        private void Start()
        {
            if (CanInitialise)
            {
                InitializeBones();
            }
            else
            {
                Debug.LogError("OVR Bones not initialised!", this);
            }
        }

        private void InitializeBones()
        {
            _bones = new List<HandBone>(ovrSkeleton.Bones.Count);
            foreach (var bone in ovrSkeleton.Bones)
            {
                if (OVRToPosingIDs.TryGetValue(bone.Id, out BoneId id))
                {
                    _bones.Add(new HandBone(id, bone.Transform));
                }
            }
            this.enabled = false;
        }

        private void Update()
        {
            if(CanInitialise)
            {
                InitializeBones();
            }
        }
    }
}