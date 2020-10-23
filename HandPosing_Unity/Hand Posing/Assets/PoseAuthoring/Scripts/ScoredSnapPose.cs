using UnityEngine;

namespace PoseAuthoring
{
    public struct ScoredSnapPose
    {
        public Pose SnapPose { get; private set; }
        public float Score { get; private set; }
        public bool IsInverted { get; private set; }

        public ScoredSnapPose(Pose pose, float score, bool isInverted)
        {
            this.SnapPose = pose;
            this.Score = score;
            this.IsInverted = isInverted;
        }

        public static ScoredSnapPose Null()
        {
            return new ScoredSnapPose(new Pose(), -1f, false);
        }

        public static bool IsNull(ScoredSnapPose pose)
        {
            return pose.Score == -1f;
        }
    }
}

