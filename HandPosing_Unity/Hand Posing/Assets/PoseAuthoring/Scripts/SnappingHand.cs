using PoseAuthoring.Grabbing;
using System;
using UnityEngine;

namespace PoseAuthoring
{
    public class SnappingHand : MonoBehaviour
    {
        [SerializeField]
        private GrabbableDetector snapDetector;
        [SerializeField]
        private Grabber grabber;
        [SerializeField]
        private GrabbableDetector detector;
        [SerializeField]
        private HandPuppet puppet;

        private HandGhost currentGhost;
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


        private void GrabStarted(Grabbable obj)
        {
            var snappable = grabber.GrabbedObject.Snappable;
            if (snappable != null)
            {
                currentGhost = snappable.FindNearsetGhost(this.puppet, out float score);
                if (currentGhost != null)
                {
                    this.puppet.SetRecordedPose(currentGhost.PoseToObject, snappable.transform, 1f, 1f);
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
                this.puppet.SetRecordedPose(currentGhost.PoseToObject, grabber.GrabbedObject.transform, 1f, 0f);
            }
        }


        private void GrabAttemp(Grabbable grabbable, float amount)
        {
            var snappable = grabber.GrabbedObject.Snappable;
            if (snappable != null)
            {
                HandGhost ghost = snappable.FindNearsetGhost(this.puppet, out float score);
                if (ghost != null)
                {
                    currentAmount = amount;
                    this.puppet.SetRecordedPose(ghost.PoseToObject, snappable.transform, amount, amount);
                }
            }
        }
    }
}
