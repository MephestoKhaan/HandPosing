using UnityEngine;

namespace HandPosing.TrackingData
{
    public abstract class SkeletonDataDecorator : SkeletonDataProvider
    {
        [SerializeField]
        protected SkeletonDataProvider wrapee;

        public override bool IsTracking => wrapee.IsTracking;

        public override float? HandScale => wrapee.HandScale;
        public override bool IsHandHighConfidence() => wrapee.IsHandHighConfidence();
        public override bool IsFingerHighConfidence(BoneId bone) => IsFingerHighConfidence(bone);
    }
}