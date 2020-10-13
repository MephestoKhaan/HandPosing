using UnityEngine;

using Grabber = Interaction.Grabber;
using Grabbable = Interaction.Grabbable;

namespace PoseAuthoring
{
    [DefaultExecutionOrder(100)]
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
        private Pose? grabOffset;

        private void OnEnable()
        {
            grabber.OnGrabAttemp += GrabAttemp;
            grabber.OnGrabStarted += GrabStarted;
            grabber.OnGrabEnded += GrabEnded;

            puppet.OnPuppetUpdated += PreAttachToObject;
        }

        private void OnDisable()
        {
            grabber.OnGrabAttemp -= GrabAttemp;
            grabber.OnGrabStarted -= GrabStarted;
            grabber.OnGrabEnded -= GrabEnded;

            puppet.OnPuppetUpdated -= PreAttachToObject;
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
                    grabbedGhost = ghost;
                    poseInVolume = grabbedGhost.AdjustPlace(bestPlace);

                    snapBack = grabbable.CanMove && snappable.HandSnapBacks;
                    grabStartTime = Time.timeSinceLevelLoad;

                    handLockFactor = 1f;
                    fingerLockFactor = 1f;

                    this.puppet.LerpOffset(poseInVolume, grabbedGhost.RelativeTo, handLockFactor);

                    grabOffset = new Pose(Quaternion.Inverse(this.puppet.TrackedPose.rotation) * (this.puppet.transform.position - this.puppet.TrackedPose.position),
                        Quaternion.Inverse(this.puppet.TrackedPose.rotation) * this.puppet.transform.rotation);
                }
            }
        }

        private void GrabEnded(Grabbable grabbable)
        {
            grabOffset = null;
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
                    handLockFactor = fingerLockFactor = 0f; 
                }
            }
        }

        private void LateUpdate()
        {
            PostAttachToObject(); 
        }

        private float AdjustSnapback(float grabStartTime)
        {
            return 1f - Mathf.Clamp01((Time.timeSinceLevelLoad - grabStartTime) / SNAPBACK_TIME);
        }


        private void PreAttachToObject()
        {
            if (grabbedGhost != null)
            {
                if (snapBack)
                {
                    handLockFactor = AdjustSnapback(grabStartTime);
                }
                this.puppet.LerpBones(poseInVolume, fingerLockFactor);

                if (grabOffset.HasValue)
                {
                    this.puppet.transform.rotation = Quaternion.Lerp(this.puppet.TrackedPose.rotation, 
                        this.puppet.TrackedPose.rotation * grabOffset.Value.rotation, 
                        handLockFactor);
                    this.puppet.transform.position = Vector3.Lerp(this.puppet.TrackedPose.position, 
                        this.puppet.TrackedPose.position + this.puppet.TrackedPose.rotation * grabOffset.Value.position, 
                        handLockFactor);
                }
                else
                {
                    this.puppet.LerpOffset(poseInVolume, grabbedGhost.RelativeTo, handLockFactor); 
                }
            }
        }

        private void PostAttachToObject()
        {
            if (grabbedGhost != null
                && grabOffset.HasValue)
            {
                this.puppet.LerpOffset(poseInVolume, grabbedGhost.RelativeTo, 1f);
            }
        }

    }
}
