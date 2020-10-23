using UnityEngine;

namespace PoseAuthoring
{
    public struct ScoredSnapPose
    {
        public HandSnapPose SnapPose { get; private set; }
        public float Score { get; private set; }
        public bool IsInverted { get; private set; }

        public ScoredSnapPose(HandSnapPose pose, float score, bool isInverted)
        {
            this.SnapPose = pose;
            this.Score = score;
            this.IsInverted = isInverted;
        }

        public static ScoredSnapPose Null()
        {
            return new ScoredSnapPose(new HandSnapPose(), -1f, false);
        }

        public static bool IsNull(ScoredSnapPose pose)
        {
            return pose.Score == -1f;
        }
    }
}

