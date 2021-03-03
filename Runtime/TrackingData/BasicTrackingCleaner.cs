
namespace HandPosing.TrackingData
{
    public class BasicTrackingCleaner : SkeletonDataDecorator
    {
        public override BonePose[] Fingers => _cleanFingers;
        public override BonePose Hand => _cleanHand;

        private BonePose[] _cleanFingers;
        private BonePose _cleanHand;

        private void OnEnable()
        {
            wrapee.OnInitialized += Initialize;
            wrapee.OnUpdated += UpdateBones;
        }

        private void OnDisable()
        {
            wrapee.OnInitialized -= Initialize;
            wrapee.OnUpdated -= UpdateBones;
        }

        private void Initialize()
        {
            _cleanFingers = (BonePose[])wrapee.Fingers.Clone();
            _cleanHand = wrapee.Hand;
        }

        private void UpdateBones(float deltaTime)
        {
            for(int i = 0; i < wrapee.Fingers.Length; i++)
            {
                BonePose rawBone = wrapee.Fingers[i];
                if (wrapee.IsFingerHighConfidence(rawBone.boneID))
                {
                    _cleanFingers[i] = rawBone;
                }
            }
            if(wrapee.IsHandHighConfidence())
            {
                _cleanHand = wrapee.Hand;
            }
        }
    }
}