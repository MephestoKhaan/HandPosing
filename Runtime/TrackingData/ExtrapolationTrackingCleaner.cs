using UnityEngine;

namespace HandPosing.TrackingData
{
    public class ExtrapolationTrackingCleaner : SkeletonDataDecorator
    {
        public override BoneRotation[] Fingers => _cleanFingers;
        public override BoneRotation Hand => _cleanHand;


        private Vector3 _handVelocity;
        private bool _wasHighConfidence;

        private BoneRotation[] _cleanFingers;
        private BoneRotation _cleanHand;

        private const float VELOCITY_SPEED = 30f;
        private const float VELOCITY_DAMPING = 5f;
        private const float CATCH_UP_TIME= 0.45f;
        private const float MAX_VELOCITY = 1f;

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
            _cleanFingers = (BoneRotation[])wrapee.Fingers.Clone();
            _cleanHand = new BoneRotation()
            {
                boneID = BoneId.Hand_Start
            };
        }

        private float? _startHighConfidenceTime;

        private void UpdateBones(float deltaTime)
        {
            for(int i = 0; i < wrapee.Fingers.Length; i++)
            {
                BoneRotation rawBone = wrapee.Fingers[i];
                if (wrapee.IsFingerHighConfidence(rawBone.boneID))
                {
                    _cleanFingers[i] = rawBone;
                }
            }

            if (wrapee.IsHandHighConfidence())
            {
                if (_wasHighConfidence)
                {
                    UpdateVelocity(_cleanHand, wrapee.Hand, deltaTime);
                }
                else
                {
                    ResetVelocity();
                    _startHighConfidenceTime = Time.timeSinceLevelLoad;
                }

                if (_startHighConfidenceTime.HasValue
                    && Time.timeSinceLevelLoad - _startHighConfidenceTime.Value < CATCH_UP_TIME)
                {
                    float t = Mathf.Clamp01((Time.timeSinceLevelLoad - _startHighConfidenceTime.Value) / CATCH_UP_TIME);
                    _cleanHand = BoneRotation.Lerp(_cleanHand, wrapee.Hand, t).Value;
                }
                else
                {
                    _startHighConfidenceTime = null;
                    _cleanHand = wrapee.Hand;
                }

                _wasHighConfidence = true;
            }
            else
            {
                _cleanHand = ApplyVelocity(_handVelocity,_cleanHand, deltaTime);
                DampVelocity(deltaTime);
                _wasHighConfidence = false;

            }
        }

        private void UpdateVelocity(BoneRotation from, BoneRotation to, float deltaTime)
        {
            Vector3 instantVelocity = (to.position - from.position) / deltaTime;
            if(instantVelocity.magnitude > MAX_VELOCITY)
            {
                instantVelocity = instantVelocity.normalized * MAX_VELOCITY;
            }
            _handVelocity = Vector3.Lerp(_handVelocity, instantVelocity, deltaTime * VELOCITY_SPEED);
        }

        private BoneRotation ApplyVelocity(Vector3 velocity, BoneRotation pose, float deltaTime)
        {
            pose.position += velocity * deltaTime;
            return pose;
        }

        private void DampVelocity(float deltaTime)
        {
            _handVelocity = Vector3.Lerp(_handVelocity, Vector3.zero, deltaTime * VELOCITY_DAMPING);
        }

        private void ResetVelocity()
        {
            _handVelocity = Vector3.zero;
        }
    }
}