

using UnityEngine;

namespace PoseAuthoring
{
    public class SnappingHand : MonoBehaviour
    {
        [SerializeField]
        private GrabbableDetector snapDetector;
        [SerializeField]
        private HandPuppet puppet;

        private void LateUpdate()
        {
            SnappableObject snappable = snapDetector.NearsestSnappable();
            if (snappable != null)
            {
                HandGhost ghost = snappable.FindNearsetGhost(this.puppet, out float score);
                if(ghost != null)
                {
                    this.puppet.SetRecordedPose(ghost.StoredPose, snappable.transform, 1f);
                }
            }
        }
    }
}
