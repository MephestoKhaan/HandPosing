using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OVRTouchSample;

namespace Interaction
{
    [RequireComponent(typeof(Grabber))]
    public class RiggedHand : MonoBehaviour
    {
        public const string ANIM_LAYER_NAME_POINT = "Point Layer";
        public const string ANIM_LAYER_NAME_THUMB = "Thumb Layer";
        public const string ANIM_PARAM_NAME_FLEX = "Flex";
        public const string ANIM_PARAM_NAME_POSE = "Pose";
        public const float THRESH_COLLISION_FLEX = 0.9f;

        public const float INPUT_RATE_CHANGE = 20.0f;

        public const float COLLIDER_SCALE_MIN = 0.01f;
        public const float COLLIDER_SCALE_MAX = 1.0f;
        public const float COLLIDER_SCALE_PER_SECOND = 1.0f;

        public const float TRIGGER_DEBOUNCE_TIME = 0.05f;
        public const float THUMB_DEBOUNCE_TIME = 0.15f;

        [SerializeField]
        private OVRInput.Controller m_controller = OVRInput.Controller.None;
        [SerializeField]
        private Animator m_animator = null;
        [SerializeField]
        private HandPose m_defaultGrabPose = null;

        private Collider[] _colliders = null;
        private bool _collisionEnabled = true;
        private Grabber _grabber;

        List<Renderer> _showAfterInputFocusAcquired;

        private int _animLayerIndexThumb = -1;
        private int _animLayerIndexPoint = -1;
        private int _animParamIndexFlex = -1;
        private int _animParamIndexPose = -1;

        private bool _isPointing = false;
        private bool _isGivingThumbsUp = false;
        private float _pointBlend = 0.0f;
        private float _thumbsUpBlend = 0.0f;

        private bool _restoreOnInputAcquired = false;

        private void Awake()
        {
            _grabber = GetComponent<Grabber>();
        }

        private void Start()
        {
            _showAfterInputFocusAcquired = new List<Renderer>();

            _colliders = this.GetComponentsInChildren<Collider>().Where(childCollider => !childCollider.isTrigger).ToArray();
            CollisionEnable(true);

            // Get animator layer indices by name, for later use switching between hand visuals
            _animLayerIndexPoint = m_animator.GetLayerIndex(ANIM_LAYER_NAME_POINT);
            _animLayerIndexThumb = m_animator.GetLayerIndex(ANIM_LAYER_NAME_THUMB);
            _animParamIndexFlex = Animator.StringToHash(ANIM_PARAM_NAME_FLEX);
            _animParamIndexPose = Animator.StringToHash(ANIM_PARAM_NAME_POSE);

            OVRManager.InputFocusAcquired += OnInputFocusAcquired;
            OVRManager.InputFocusLost += OnInputFocusLost;
        }

        private void OnDestroy()
        {
            OVRManager.InputFocusAcquired -= OnInputFocusAcquired;
            OVRManager.InputFocusLost -= OnInputFocusLost;
        }

        private void Update()
        {
            UpdateCapTouchStates();

            _pointBlend = InputValueRateChange(_isPointing, _pointBlend);
            _thumbsUpBlend = InputValueRateChange(_isGivingThumbsUp, _thumbsUpBlend);

            UpdateAnimStates();
        }

        // Just checking the state of the index and thumb cap touch sensors, but with a little bit of
        // debouncing.
        private void UpdateCapTouchStates()
        {
            _isPointing = !OVRInput.Get(OVRInput.NearTouch.PrimaryIndexTrigger, m_controller);
            _isGivingThumbsUp = !OVRInput.Get(OVRInput.NearTouch.PrimaryThumbButtons, m_controller);
        }

        private void LateUpdate()
        {
            // Hand's collision grows over a short amount of time on enable, rather than snapping to on, to help somewhat with interpenetration issues.
            if (_collisionEnabled && m_collisionScaleCurrent + Mathf.Epsilon < COLLIDER_SCALE_MAX)
            {
                m_collisionScaleCurrent = Mathf.Min(COLLIDER_SCALE_MAX, m_collisionScaleCurrent + Time.deltaTime * COLLIDER_SCALE_PER_SECOND);
                for (int i = 0; i < _colliders.Length; ++i)
                {
                    Collider collider = _colliders[i];
                    collider.transform.localScale = new Vector3(m_collisionScaleCurrent, m_collisionScaleCurrent, m_collisionScaleCurrent);
                }
            }
        }

        // Simple Dash support. Just hide the hands.
        private void OnInputFocusLost()
        {
            if (gameObject.activeInHierarchy)
            {
                _showAfterInputFocusAcquired.Clear();
                Renderer[] renderers = GetComponentsInChildren<Renderer>();
                for (int i = 0; i < renderers.Length; ++i)
                {
                    if (renderers[i].enabled)
                    {
                        renderers[i].enabled = false;
                        _showAfterInputFocusAcquired.Add(renderers[i]);
                    }
                }
                _restoreOnInputAcquired = true;
            }
        }

        private void OnInputFocusAcquired()
        {
            if (_restoreOnInputAcquired)
            {
                for (int i = 0; i < _showAfterInputFocusAcquired.Count; ++i)
                {
                    if (_showAfterInputFocusAcquired[i])
                    {
                        _showAfterInputFocusAcquired[i].enabled = true;
                    }
                }
                _showAfterInputFocusAcquired.Clear();

                _restoreOnInputAcquired = false;
            }
        }

        private float InputValueRateChange(bool isDown, float value)
        {
            float rateDelta = Time.deltaTime * INPUT_RATE_CHANGE;
            float sign = isDown ? 1.0f : -1.0f;
            return Mathf.Clamp01(value + rateDelta * sign);
        }

        private void UpdateAnimStates()
        {
            bool grabbing = _grabber.GrabbedObject != null;
            HandPose grabPose = m_defaultGrabPose;
            if (grabbing)
            {
                HandPose customPose = _grabber.GrabbedObject.GetComponent<HandPose>();
                if (customPose != null) grabPose = customPose;
            }
            // Pose
            HandPoseId handPoseId = grabPose.PoseId;
            m_animator.SetInteger(_animParamIndexPose, (int)handPoseId);

            // Flex
            // blend between open hand and fully closed fist
            float flex = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, m_controller);
            m_animator.SetFloat(_animParamIndexFlex, flex);

            // Point
            bool canPoint = !grabbing || grabPose.AllowPointing;
            float point = canPoint ? _pointBlend : 0.0f;
            m_animator.SetLayerWeight(_animLayerIndexPoint, point);

            // Thumbs up
            bool canThumbsUp = !grabbing || grabPose.AllowThumbsUp;
            float thumbsUp = canThumbsUp ? _thumbsUpBlend : 0.0f;
            m_animator.SetLayerWeight(_animLayerIndexThumb, thumbsUp);

            float pinch = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, m_controller);
            m_animator.SetFloat("Pinch", pinch);
        }

        private float m_collisionScaleCurrent = 0.0f;

        private void CollisionEnable(bool enabled)
        {
            if (_collisionEnabled == enabled)
            {
                return;
            }
            _collisionEnabled = enabled;

            if (enabled)
            {
                m_collisionScaleCurrent = COLLIDER_SCALE_MIN;
                for (int i = 0; i < _colliders.Length; ++i)
                {
                    Collider collider = _colliders[i];
                    collider.transform.localScale = new Vector3(COLLIDER_SCALE_MIN, COLLIDER_SCALE_MIN, COLLIDER_SCALE_MIN);
                    collider.enabled = true;
                }
            }
            else
            {
                m_collisionScaleCurrent = COLLIDER_SCALE_MAX;
                for (int i = 0; i < _colliders.Length; ++i)
                {
                    Collider collider = _colliders[i];
                    collider.enabled = false;
                    collider.transform.localScale = new Vector3(COLLIDER_SCALE_MIN, COLLIDER_SCALE_MIN, COLLIDER_SCALE_MIN);
                }
            }
        }
    }
}
