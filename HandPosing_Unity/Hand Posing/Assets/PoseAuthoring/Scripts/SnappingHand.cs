

using UnityEngine;

namespace PoseAuthoring
{
    public class SnappingHand : MonoBehaviour
    {
        [SerializeField]
        private GrabbableDetector snapDetector;
        [SerializeField]
        private OVRGrabber grabber;
        [SerializeField]
        private HandPuppet puppet;

        private bool _isGrabbing;

        private void LateUpdate()
        {
            if(!_isGrabbing 
                && grabber.grabbedObject != null)
            {
                _isGrabbing = true;
               
                if (grabber.grabbedObject.TryGetComponent<SnappableObject>(out SnappableObject snappable))
                {
                    HandGhost ghost = snappable.FindNearsetGhost(this.puppet, out float score);
                    if (ghost != null)
                    {
                        this.puppet.SetRecordedPose(ghost.PoseToObject, snappable.transform, 1f);
                    }
                }
            }
        }
    }
}
