using PoseAuthoring.Adapters;
using UnityEngine;

namespace PoseAuthoring.OVRIntegration
{
    public class OVRAnchorsUpdateNotifier : AnchorsUpdateNotifier
    {
        [SerializeField]
        OVRCameraRig ovrCameraRig;

        private bool _usingOVRUpdates;

        private void Awake()
        {
            InitializeOVRUpdates();
        }

        protected override void Update()
        {
            if (!_usingOVRUpdates)
            {
                OnAnchorsUpdated?.Invoke();
            }
        }

        private void InitializeOVRUpdates()
        {
            if (ovrCameraRig != null)
            {
                ovrCameraRig.UpdatedAnchors += (r) =>
                {
                    OnAnchorsUpdated?.Invoke();
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