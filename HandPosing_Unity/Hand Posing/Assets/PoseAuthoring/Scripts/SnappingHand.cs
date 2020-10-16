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

        private Pose? grabOffset = null;

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
                HandGhost ghost = snappable.FindBestGhost(userPose, out float score, out var bestPlace);

                if (ghost != null)
                {
                    grabbedGhost = ghost;
                    poseInVolume = grabbedGhost.AdjustPlace(bestPlace);

                    snapBack = grabbable.CanMove && snappable.HandSnapBacks;
                    grabStartTime = Time.timeSinceLevelLoad;

                    handLockFactor = fingerLockFactor = 1f;

                    this.puppet.LerpOffset(poseInVolume, grabbedGhost.RelativeTo, handLockFactor);

                }
            }
        }

        private void GrabEnded(Grabbable grabbable)
        {
            grabbedGhost = null;
            snapBack = false;
            grabOffset = null;
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

        private void LateUpdate()
        {
           PostAttachToObject();

            grabOffset = this.puppet.Anchor.RelativeOffset(this.puppet.transform);
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
                    Quaternion rot = Quaternion.Lerp(
                         this.puppet.transform.rotation,
                         this.puppet.Anchor.rotation * grabOffset.Value.rotation, 
                         handLockFactor);

                    Vector3 pos = Vector3.Lerp(
                        this.puppet.transform.position,
                        this.puppet.Anchor.position + this.puppet.Anchor.rotation * grabOffset.Value.position, 
                        handLockFactor);

                    this.transform.SetPositionAndRotation(pos, rot);
                } 
                else
                {
                    this.puppet.LerpOffset(poseInVolume, grabbedGhost.RelativeTo, handLockFactor);
                }
            }
        }

        private void PostAttachToObject()
        {
            if (grabOffset.HasValue 
                && grabbedGhost != null)
            {
                this.puppet.LerpOffset(poseInVolume, grabbedGhost.RelativeTo, 1f);
            }
        }

        private float AdjustSnapback(float grabStartTime)
        {
            return 1f - Mathf.Clamp01((Time.timeSinceLevelLoad - grabStartTime) / SNAPBACK_TIME);
        }

    }
}
