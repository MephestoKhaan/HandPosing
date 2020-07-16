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

        private const float SNAPBACK_TIME = 0.4f;

        private HandGhost currentGhost;
        private HandSnapPose poseInVolume;
        private float grabbingAmount;
        private float offsetAmount;
        private float grabStartTime;
        private bool snapBack;

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
                    poseInVolume = currentGhost.AdjustPlace(bestPlace);
                    grabbingAmount = 1f;
                    offsetAmount = 1f;
                    grabStartTime = Time.timeSinceLevelLoad;
                    snapBack = grabbable.CanMove && snappable.HandSnapBacks;
                    this.puppet.TransitionToPose(poseInVolume, currentGhost.RelativeTo, grabbingAmount, 1f);

                }
            }
        }

        private void GrabEnded(Grabbable obj)
        {
            currentGhost = null;
            snapBack = false;
        }

        private void Snap()
        {
            if (currentGhost != null)
            {
                if(snapBack)
                {
                    offsetAmount = 1f - Mathf.Clamp01((Time.timeSinceLevelLoad - grabStartTime) / SNAPBACK_TIME);
                }

                this.puppet.TransitionToPose(poseInVolume, currentGhost.RelativeTo, grabbingAmount, offsetAmount);
            }
        }

        private void GrabAttemp(Grabbable grabbable, float amount)
        {
            snapBack = false;
            if (grabbable == null)
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
                    poseInVolume = currentGhost.AdjustPlace(bestPlace);
                    offsetAmount = grabbingAmount = amount;
                }
                else
                {
                    currentGhost = null;
                    offsetAmount = grabbingAmount = 0f;
                }
            }
        }
    }
}
