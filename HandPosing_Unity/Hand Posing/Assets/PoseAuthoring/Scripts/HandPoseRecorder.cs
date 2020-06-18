using UnityEngine;

namespace PoseAuthoring
{
    public class HandPoseRecorder : MonoBehaviour
    {
        [SerializeField]
        private HandPuppet puppetHand;
        [SerializeField]
        private GrabbableDetector grabber;

        private HandGhost previousGhost;

        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.Space))
            {
                RecordPose();
            }

            HighlightNearestPose();
        }

        private void HighlightNearestPose()
        {
            SnappableObject snappable = grabber.NearsestSnappable();
            if (snappable != null)
            {
                HandGhost ghost = snappable.FindNearsetGhost(this.puppetHand);
                if (ghost != previousGhost)
                {
                    previousGhost?.Highlight(false);
                    ghost?.Highlight(true);
                    previousGhost = ghost;
                }
            }
            else if (previousGhost != null)
            {
                previousGhost.Highlight(false);
                previousGhost = null;
            }
        }

        public void RecordPose()
        {
            SnappableObject snappable = grabber.NearsestSnappable();
            snappable?.AddPose(puppetHand);
        }
    }
}