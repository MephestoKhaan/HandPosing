using UnityEngine;

namespace PoseAuthoring.Adapters.OVRIntegration
{
    public class OVRUpdateNotifier : AnchorsUpdateNotifier
    {
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