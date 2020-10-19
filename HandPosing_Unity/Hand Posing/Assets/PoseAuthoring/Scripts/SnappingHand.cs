using UnityEngine;

using Grabber = Interaction.Grabber;
using Grabbable = Interaction.Grabbable;

namespace PoseAuthoring
{
    [DefaultExecutionOrder(50)]
    public class SnappingHand : MonoBehaviour
    {
        [SerializeField]
        private Grabber grabber;
        [SerializeField]
        private HandPuppet puppet;

        private const float SNAPBACK_TIME = 0.33f;

        private HandGhost grabbedGhost;
        private HandSnapPose poseInVolume;
        private float fingerLockFactor;
        private float handLockFactor;
        private float grabStartTime;
        private bool snapBack;
        private bool isGrabbing;

        private Pose currentOffset;

        private Pose? grabOffset;

        private void OnEnable()
        {
            grabber.OnGrabAttemp += GrabAttemp;
            grabber.OnGrabStarted += GrabStarted;
            grabber.OnGrabEnded += GrabEnded;

            puppet.OnPuppetPreUpdate += PreAttachToObject;
            puppet.OnPuppetUpdated += AttachToObjectOffseted;
            Application.onBeforeRender += PostAttachToObject;
        }

        private void OnDisable()
        {
            grabber.OnGrabAttemp -= GrabAttemp;
            grabber.OnGrabStarted -= GrabStarted;
            grabber.OnGrabEnded -= GrabEnded;

            puppet.OnPuppetPreUpdate -= PreAttachToObject;
            puppet.OnPuppetUpdated -= AttachToObjectOffseted;
            Application.onBeforeRender -= PostAttachToObject;
        }


        private void GrabStarted(Grabbable grabbable)
        {
            SnappableObject snappable = grabbable.Snappable;

            if (snappable != null)
            {
                HandSnapPose userPose = this.puppet.CurrentPoseTracked(snappable.transform);
                HandGhost ghost = snappable.FindBestGhost(userPose, out float score, out var bestPlace);

                if (ghost != null)
                {
                    grabbedGhost = ghost;
                    poseInVolume = grabbedGhost.AdjustPlace(bestPlace);

                    snapBack = snappable.HandSnapBacks;
                    grabStartTime = Time.timeSinceLevelLoad;

                    handLockFactor = fingerLockFactor = 1f;

                    this.puppet.LerpOffset(poseInVolume, grabbedGhost.RelativeTo, handLockFactor);

                    grabOffset = new Pose(this.transform.localPosition, this.transform.localRotation);
                    isGrabbing = true;
                }
            }
        }

        private void GrabEnded(Grabbable grabbable)
        {
            isGrabbing = false;
            grabbedGhost = null;
            snapBack = false;
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
                HandGhost ghost = snappable.FindBestGhost(userPose, out float score, out var bestPlace);
                if (ghost != null)
                {
                    grabbedGhost = ghost;
                    poseInVolume = grabbedGhost.AdjustPlace(bestPlace);
                    handLockFactor = fingerLockFactor = amount;
                }
                else
                {
                    grabbedGhost = null;
                    handLockFactor = fingerLockFactor = 0f;
                }
            }
        }

        private void PreAttachToObject()
        {
            if (isGrabbing)
            {
                //moves it back into place
                offset.localRotation = currentOffset.rotation;
                offset.localPosition = currentOffset.position;
            }
        }

        private void AttachToObjectOffseted()
        {
            if (grabbedGhost != null)
            {
                if (snapBack)
                {
                    handLockFactor = AdjustSnapbackTime(grabStartTime);
                }
                
                this.puppet.LerpBones(poseInVolume, fingerLockFactor);

                if (isGrabbing 
                    && !snapBack)
                {
                    this.transform.localRotation = currentOffset.rotation;
                    this.transform.localPosition = currentOffset.position;
                } 
                else if(isGrabbing 
                    && snapBack)
                {
                    this.transform.localRotation = Quaternion.Lerp(this.transform.localRotation, grabOffset.Value.rotation, handLockFactor);
                    this.transform.localPosition = Vector3.Lerp(this.transform.localPosition, grabOffset.Value.position, handLockFactor);
                }
                else
                {
                    this.puppet.LerpOffset(poseInVolume, grabbedGhost.RelativeTo, handLockFactor);
                }
            }
        }

        public Transform offset;


        private void PostAttachToObject()
        {
            if (isGrabbing
                && grabbedGhost != null)
            {
                currentOffset = new Pose(offset.localPosition, offset.localRotation);
                this.puppet.LerpOffset(poseInVolume, grabbedGhost.RelativeTo, 1f, offset);
            }
        }

        private float AdjustSnapbackTime(float grabStartTime)
        {
            return 1f - Mathf.Clamp01((Time.timeSinceLevelLoad - grabStartTime) / SNAPBACK_TIME);
        }
    }
}
