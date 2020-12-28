using UnityEngine;

namespace HandPosing.OVRIntegration
{
    /// <summary>
    /// This is a custom implementation for Oculus plugin of the AnchorsUpdateNotifier.
    /// Since OVRCameraRig sometimes updates anchors at Update, FixedUpdate or even OnBeforeRender,
    /// this will inform the puppet system of the right moments these updates occur.
    /// </summary>
    public class UpdateNotifierOVR : AnchorsUpdateNotifier
    {
        /// <summary>
        /// The player Camera Rig, where the update mode is set.
        /// </summary>
        [SerializeField]
        private OVRCameraRig ovrCameraRig;

        private bool _usingOVRUpdates;
        private bool _alreadyUpdated;

        private void Reset()
        {
            ovrCameraRig = this.GetComponentInParent<OVRCameraRig>();
        }

        private void Awake()
        {
            InitializeOVRUpdates();
        }

        protected override void Update()
        {
            _alreadyUpdated = false;
            if (!_usingOVRUpdates)
            {
                OnAnchorsFirstUpdate?.Invoke();
                OnAnchorsEveryUpdate?.Invoke();
            }
        }

        private void AnchorsUpdated()
        {
            if (!_alreadyUpdated)
            {
                _alreadyUpdated = true;
                OnAnchorsFirstUpdate?.Invoke();
            }
            OnAnchorsEveryUpdate?.Invoke();
        }

        private void InitializeOVRUpdates()
        {
            if (ovrCameraRig != null)
            {
                ovrCameraRig.UpdatedAnchors += (r) =>
                {
                    AnchorsUpdated();
                };
                _usingOVRUpdates = true;
            }
            else
            {
                _usingOVRUpdates = false;
            }
        }
    }
}