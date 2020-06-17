using UnityEngine;

namespace PoseAuthoring
{
    public class HandPoseRecorder : MonoBehaviour
    {
        [SerializeField]
        private HandPuppet puppetHand;
        [SerializeField]
        private GrabbableDetector grabber;

        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.Space))
            {
                RecordPose();
            }
        }
        public void RecordPose()
        {
            SnappableObject snappable = grabber.NearsestSnappable();
            if(snappable != null)
            {
                snappable.AddPose(puppetHand);
            }
        }
    }
}