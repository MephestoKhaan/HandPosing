using PoseAuthoring.Grabbing;
using UnityEngine;

namespace PoseAuthoring
{
    public class SnappingHand : MonoBehaviour
    {
        [SerializeField]
        private Grabber grabber;
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


        private void GrabStarted(Grabbable grabbable)
        {
            SnappableObject snappable = grabbable.Snappable;
            if (snappable != null)
            {
                currentGhost = snappable.FindNearsetGhost(this.puppet, out float score);
                if (currentGhost != null)
                {
                    this.puppet.SetRecordedPose(currentGhost.PoseToObject, snappable.transform, 1f, 1f);
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
                this.puppet.SetRecordedPose(currentGhost.PoseToObject, currentGhost.RelativeTo.transform, currentAmount, currentAmount);
            }
        }


        private void GrabAttemp(Grabbable grabbable, float amount)
        {
            if(grabbable == null)
            {
                currentGhost = null;
                return;
            }
            SnappableObject snappable = grabbable.Snappable;
            if (snappable != null)
            {
                currentGhost = snappable.FindNearsetGhost(this.puppet, out float score);
                currentAmount = amount;
            }
        }
    }
}
