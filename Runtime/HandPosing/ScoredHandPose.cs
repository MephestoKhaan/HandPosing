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

        public static ScoredHandPose? Lerp(ScoredHandPose from, ScoredHandPose to, float t)
        {
            if(from.Direction != to.Direction)
            {
                UnityEngine.Debug.LogError("ScoredHandPose must have same direction for interpolation");
                return null;
            }

            float score = UnityEngine.Mathf.Lerp(from.Score, to.Score, t);
            HandPose? pose = HandPose.Lerp(from.Pose, to.Pose, t);
            if (!pose.HasValue)
            {
                UnityEngine.Debug.LogError("ScoredHandPose interpolation error");
                return null;
            }
            return new ScoredHandPose(pose.Value, score, from.Direction);
        }
    }
}

