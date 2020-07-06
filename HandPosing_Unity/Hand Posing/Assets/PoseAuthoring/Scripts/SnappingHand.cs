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

            puppet.OnUpdated += Snap;
        }

        private void OnDisable()
        {
            grabber.OnGrabAttemp -= GrabAttemp;
            grabber.OnGrabStarted -= GrabStarted;
            grabber.OnGrabEnded -= GrabEnded;

            puppet.OnUpdated -= Snap;
        }

        private void GrabStarted(Grabbable grabbable)
        {
            SnappableObject snappable = grabbable.Snappable;
            if (snappable != null)
            {
                HandSnapPose userPose = this.puppet.CurrentPoseTracked(snappable.transform);
                HandGhost ghost = snappable.FindNearsetGhost(userPose, out float score, out var bestPlace);
                if (ghost != null)
                {
                    currentGhost = ghost;
                    currentPose = currentGhost.AdjustPlace(bestPlace);
                    currentAmount = 1f;
                    this.puppet.TransitionToPose(currentPose, currentGhost.RelativeTo, currentAmount, currentAmount);
                }
            }
        }

        private void GrabEnded(Grabbable obj)
        {
            currentGhost = null;
        }

        private void Snap()
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
               
                HandGhost ghost = snappable.FindNearsetGhost(userPose, out float score, out var bestPlace);
                if (ghost != null)
                {
                    currentGhost = ghost;
                    currentPose = currentGhost.AdjustPlace(bestPlace);
                    currentAmount = amount;
                }
                else
                {
                    currentGhost = null;
                    currentAmount = 0f;
                }
            }
        }
    }
}
