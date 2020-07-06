using UnityEngine;

using Grabber = Interaction.Grabber;
using Grabbable = Interaction.Grabbable;

namespace PoseAuthoring
{
    public class SnappingHand : MonoBehaviour
    {
        [SerializeField]
        private Grabber grabber;
        [SerializeField]
        private HandPuppet puppet;

        private HandGhost currentGhost;
        private HandSnapPose currentPose;
        private float currentAmount;

        private void OnEnable()
        {
            grabber.OnGrabAttemp += GrabAttemp;
            grabber.OnGrabStarted += GrabStarted;
            grabber.OnGrabEnded += GrabEnded;
        }

        private void OnDisable()
        {
            grabber.OnGrabAttemp -= GrabAttemp;
            grabber.OnGrabStarted -= GrabStarted;
            grabber.OnGrabEnded -= GrabEnded;
        }

        private void GrabStarted(Grabbable grabbable)
        {
            SnappableObject snappable = grabbable.Snappable;
            if (snappable != null)
            {
                HandSnapPose userPose = this.puppet.CurrentPoseTracked(snappable.transform);
                currentGhost = snappable.FindNearsetGhost(userPose, out float score, out var bestPose);

                if (currentGhost != null)
                {
                    currentPose = currentGhost.AdjustPose(bestPose);
                    this.puppet.TransitionToPose(currentPose, snappable.transform, 1f, 1f);
                    currentAmount = 1f;
                }
            }
        }

        private void GrabEnded(Grabbable obj)
        {
            currentGhost = null;
        }

        private void LateUpdate()
        {
            if (currentGhost != null)
            {
                this.puppet.TransitionToPose(currentPose, currentGhost.RelativeTo, currentAmount, currentAmount);
            }
        }

        private void GrabAttemp(Grabbable grabbable, float amount)
        {
            if(grabbable == null)
            {
                currentGhost = null;
                this.puppet.SetDefaultPose();
                return;
            }
            SnappableObject snappable = grabbable.Snappable;
            if (snappable != null)
            {
                HandSnapPose userPose = this.puppet.CurrentPoseTracked(snappable.transform);
               
                currentGhost = snappable.FindNearsetGhost(userPose, out float score, out var bestPose);
                if (currentGhost != null)
                {
                    currentPose = currentGhost.AdjustPose(bestPose);
                    currentAmount = amount;
                }
                else
                {
                    currentAmount = 0f;
                }
            }
        }
    }
}
