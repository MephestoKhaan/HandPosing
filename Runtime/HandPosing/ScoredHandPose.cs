namespace HandPosing
{
    /// <summary>
    /// Direction at which the a snap happened.
    /// </summary>
    public enum SnapDirection
    {
        Any,
        Forward,
        Backward,
        None
    }; 

    /// <summary>
    /// A merge between a HandPose and the score indicating how good it is for snapping an object.
    /// </summary>
    public struct ScoredHandPose
    {
        /// <summary>
        /// The HandPose beign scored.
        /// </summary>
        public HandPose Pose { get; private set; }

        /// <summary>
        /// The direction in which the snap is happening.
        /// </summary>
        public SnapDirection Direction { get; private set; }
        /// <summary>
        /// The score of the snap. 
        /// -1 for an invalid pose
        /// 0 for a bad snapping (too far away)
        /// 1 for a perfect snapping.
        /// </summary>
        public float Score { get; private set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="pose">The HandPose to measure.</param>
        /// <param name="score">Score of the snap.</param>
        /// <param name="direction">Direction of the snap.</param>
        public ScoredHandPose(HandPose pose, float score, SnapDirection direction)
        {
            this.Pose = pose;
            this.Score = score;
            this.Direction = direction;
        }

        /// <summary>
        /// Default empty ScoredHandPose.
        /// </summary>
        /// <returns>A ScoredHandPose with invalid score.</returns>
        public static ScoredHandPose Null()
        {
            return new ScoredHandPose(new HandPose(), -1f, SnapDirection.None);
        }

        /// <summary>
        /// Check if the given ScoredHandPose is valid.
        /// </summary>
        /// <param name="pose">The ScoredHandPose to check.</param>
        /// <returns>True for an invalid pose.</returns>
        public static bool IsNull(ScoredHandPose pose)
        {
            return pose.Score == -1f;
        }

        /// <summary>
        /// Interpolate between two ScoredHandPose. Both ScoredHandPoses must have the same direction.
        /// This method does not only moves the hands, but also adjusts the score linearly.
        /// </summary>
        /// <param name="from">The base ScoredHandPose to interpolate from.</param>
        /// <param name="to">The target ScoredHandPose to interpolate to.</param>
        /// <param name="t">The interpolation factor, 0 for the base, 1 for the target value.</param>
        /// <returns>A ScoredHandPose between base and target, null if they are not interpolable.</returns>
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

