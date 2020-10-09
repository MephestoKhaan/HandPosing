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
        private float fingerLockFactor;
        private float handLockFactor;
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
                grabbable.OnMoved += SnapToGrabbable;
                

                HandSnapPose userPose = this.puppet.CurrentPoseTracked(snappable.transform);
                HandGhost ghost = snappable.FindNearsetGhost(userPose, out float score, out var bestPlace);

                if (ghost != null)
                {
                    grabbedGhost = ghost;
                    poseInVolume = grabbedGhost.AdjustPlace(bestPlace);

                    snapBack = grabbable.CanMove && snappable.HandSnapBacks;
                    grabStartTime = Time.timeSinceLevelLoad;

                    handLockFactor = 1f;
                    fingerLockFactor = 1f;
                    this.puppet.TransitionToPose(poseInVolume, grabbedGhost.RelativeTo, fingerLockFactor, handLockFactor);
                }
            }
        }

        private void GrabEnded(Grabbable grabbable)
        {
            grabbable.OnMoved -= SnapToGrabbable;

            grabbedGhost = null;
            snapBack = false;
        }

        private void SnapToGrabbable()
        {
            if (grabbedGhost != null)
            {
                if(snapBack)
                {
                    handLockFactor = 1f - Mathf.Clamp01((Time.timeSinceLevelLoad - grabStartTime) / SNAPBACK_TIME);
                }
                this.puppet.TransitionToPose(poseInVolume, grabbedGhost.RelativeTo, fingerLockFactor, handLockFactor);
            }
        }

        private void GrabAttemp(Grabbable grabbable, float amount)
        {
            snapBack = false;
            if (grabbable == null)
            {
                grabbedGhost = null;
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
                    handLockFactor = fingerLockFactor = amount;
                }
                else
                {
                    grabbedGhost = null;
                    handLockFactor = fingerLockFactor = 0f; //TODO: animate?
                }
            }
        }
    }
}
