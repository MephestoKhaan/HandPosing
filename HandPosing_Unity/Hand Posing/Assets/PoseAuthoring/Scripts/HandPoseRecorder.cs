using PoseAuthoring.Grabbing;
using UnityEngine;

namespace PoseAuthoring
{
    public class HandPoseRecorder : MonoBehaviour
    {
        [SerializeField]
        private HandPuppet puppetHand;
        [SerializeField]
        private Grabber grabber;

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
            var grabbable = grabber.FindClosestGrabbable().Item1;
            
            if (grabbable != null && grabbable.Snappable != null)
            {
                HandGhost ghost = grabbable.Snappable.FindNearsetGhost(this.puppetHand, out float score);
                if (ghost != previousGhost)
                {
                    previousGhost?.Highlight(false);
                    previousGhost = ghost;
                }
                ghost?.Highlight(score);
            }
            else if (previousGhost != null)
            {
                previousGhost.Highlight(false);
                previousGhost = null;
            }
        }

        public void RecordPose()
        {
            Grabbable grabbable = grabber.FindClosestGrabbable().Item1;
            grabbable?.Snappable?.AddPose(puppetHand);
        }
    }
}