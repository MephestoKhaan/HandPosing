using UnityEngine;

namespace PoseAuthoring
{
    public struct ScoredSnapPose
    {
        public enum SnapDirection
        {
            Any,
            Forward,
            Backward,
            None
        }

        public HandSnapPose SnapPose { get; private set; }
        public float Score { get; private set; }
        public SnapDirection Direction { get; private set; }

        public ScoredSnapPose(HandSnapPose pose, float score, SnapDirection direction)
        {
            this.SnapPose = pose;
            this.Score = score;
            this.Direction = direction;
        }

        public static ScoredSnapPose Null()
        {
            return new ScoredSnapPose(new HandSnapPose(), -1f, SnapDirection.None);
        }

        public static bool IsNull(ScoredSnapPose pose)
        {
            return pose.Score == -1f;
        }
    }
}

