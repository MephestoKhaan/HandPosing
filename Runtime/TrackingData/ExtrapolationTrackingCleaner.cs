using UnityEngine;

namespace HandPosing.TrackingData
{
    public class ExtrapolationTrackingCleaner : SkeletonDataDecorator
    {
        public override BonePose[] Fingers => _cleanFingers;
        public override BonePose Hand => _cleanHand;


        private Vector3 _handVelocity;
        private bool _wasHighConfidence;
        private bool _ready;

        private BonePose[] _cleanFingers;
        private BonePose _cleanHand;

        private const float VELOCITY_SPEED = 30f;
        private const float VELOCITY_DAMPING = 5f;
        private const float CATCH_UP_TIME = 0.45f;
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
            _cleanFingers = (BonePose[])wrapee.Fingers.Clone();
            _cleanHand = new BonePose()
            {
                boneID = BoneId.Hand_Start,
                rotation = wrapee.Hand.rotation,
                position = wrapee.Hand.position
            };
        }

        private float? _startHighConfidenceTime;

        private void UpdateBones(float deltaTime)
        {

            if (wrapee.IsHandHighConfidence())
            {
                _ready = true;
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
                    _cleanHand = BonePose.Lerp(_cleanHand, wrapee.Hand, t).Value;
                }
                else
                {
                    _startHighConfidenceTime = null;
                    _cleanHand = wrapee.Hand;
                }

                _wasHighConfidence = true;
            }
            else if (_ready)
            {
                _cleanHand = ApplyVelocity(_handVelocity, _cleanHand, deltaTime);
                DampVelocity(deltaTime);
                _wasHighConfidence = false;
            }
            else
            {
                _cleanHand.rotation = wrapee.Hand.rotation;
                _cleanHand.position = wrapee.Hand.position;
            }

            for (int i = 0; i < wrapee.Fingers.Length; i++)
            {
                BonePose rawBone = wrapee.Fingers[i];
                if (wrapee.IsFingerHighConfidence(rawBone.boneID)
                    || !_ready)
                {
                    _cleanFingers[i] = rawBone;
                }
            }
        }

        private void UpdateVelocity(BonePose from, BonePose to, float deltaTime)
        {
            Vector3 instantVelocity = (to.position - from.position) / deltaTime;
            if (instantVelocity.magnitude > MAX_VELOCITY)
            {
                instantVelocity = instantVelocity.normalized * MAX_VELOCITY;
            }
            _handVelocity = Vector3.Lerp(_handVelocity, instantVelocity, deltaTime * VELOCITY_SPEED);
        }

        private BonePose ApplyVelocity(Vector3 velocity, BonePose pose, float deltaTime)
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