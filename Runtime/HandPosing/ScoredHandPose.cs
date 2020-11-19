namespace HandPosing
{
    public enum SnapDirection
    {
        Any,
        Forward,
        Backward,
        None
    }; 

    public struct ScoredHandPose
    {
        public HandPose Pose { get; private set; }

        public SnapDirection Direction { get; private set; }
        public float Score { get; private set; }

        public ScoredHandPose(HandPose pose, float score, SnapDirection direction)
        {
            this.Pose = pose;
            this.Score = score;
            this.Direction = direction;
        }

        public static ScoredHandPose Null()
        {
            return new ScoredHandPose(new HandPose(), -1f, SnapDirection.None);
        }

        public static bool IsNull(ScoredHandPose pose)
        {
            return pose.Score == -1f;
        }
    }
}

