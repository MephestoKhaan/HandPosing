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

        private Pose lastGlobalPose;

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

        private void LateUpdate()
        {
            PostAttachToObject();
            lastGlobalPose = new Pose(this.transform.localPosition, this.transform.localRotation);
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
                this.puppet.LerpOffset(poseInVolume, grabbedGhost.RelativeTo, handLockFactor);

                if (isGrabbing)
                {
                    Quaternion rot = Quaternion.Lerp(transform.localRotation, lastGlobalPose.rotation, handLockFactor);
                    Vector3 pos = Vector3.Lerp(transform.localPosition, lastGlobalPose.position, handLockFactor);
                    this.transform.localRotation = rot;
                    this.transform.localPosition = pos;
                }
            }
        }

        private void PostAttachToObject()
        {
            if (isGrabbing
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
