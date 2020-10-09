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

        private HandGhost grabbedGhost;
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

            puppet.OnPostupdated += SnapToGrabbable;
        }

        private void OnDisable()
        {
            grabber.OnGrabAttemp -= GrabAttemp;
            grabber.OnGrabStarted -= GrabStarted;
            grabber.OnGrabEnded -= GrabEnded;

            puppet.OnPostupdated -= SnapToGrabbable;
        }

        private void GrabStarted(Grabbable grabbable)
        {
            SnappableObject snappable = grabbable.Snappable;
            if (snappable != null)
            {
                //puppet.OnPostupdated -= SnapToGrabbable;
                grabbable.OnMoved += SnapToGrabbable;

                HandSnapPose userPose = this.puppet.CurrentPoseTracked(snappable.transform);
                HandGhost ghost = snappable.FindNearsetGhost(userPose, out float score, out var bestPlace);

                if (ghost != null)
                {
                    grabbedGhost = ghost;
                    poseInVolume = grabbedGhost.AdjustPlace(bestPlace);
                    grabbingAmount = 1f;
                    offsetAmount = 1f;
                    grabStartTime = Time.timeSinceLevelLoad;
                    snapBack = grabbable.CanMove && snappable.HandSnapBacks;
                    this.puppet.TransitionToPose(poseInVolume, grabbedGhost.RelativeTo, grabbingAmount, offsetAmount);
                }
            }
        }

        private void GrabEnded(Grabbable grabbable)
        {
            grabbable.OnMoved -= SnapToGrabbable;
            //puppet.OnPostupdated += SnapToGrabbable;

            grabbedGhost = null;
            snapBack = false;
        }

        private void SnapToGrabbable()
        {
            if (grabbedGhost != null)
            {
                if(snapBack)
                {
                    offsetAmount = 1f - Mathf.Clamp01((Time.timeSinceLevelLoad - grabStartTime) / SNAPBACK_TIME);
                }
                this.puppet.TransitionToPose(poseInVolume, grabbedGhost.RelativeTo, grabbingAmount, offsetAmount);
            }
        }

        private void GrabAttemp(Grabbable grabbable, float amount)
        {
            snapBack = false;
            if (grabbable == null)
            {
                grabbedGhost = null;
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
                    grabbedGhost = ghost;
                    poseInVolume = grabbedGhost.AdjustPlace(bestPlace);
                    offsetAmount = grabbingAmount = amount;
                    
                }
                else
                {
                    grabbedGhost = null;
                    offsetAmount = grabbingAmount = 0f;
                }
            }
        }
    }
}
