using UnityEngine;

namespace HandPosing.SnapRecording
{
    /// <summary>
    /// A static (non-user controller) representation of a hand. This script is used
    /// to be able to manually visualize and modify hand poses.
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(HandPuppet))]
    public class HandGhost : MonoBehaviour
    {
        /// <summary>
        /// The puppet is used to actually move the represention of the hand.
        /// </summary>
        [SerializeField]
        private HandPuppet puppet;

        /// <summary>
        /// Called everytime the transforms or the fingers are manually moved, so the
        /// data can be updated.
        /// </summary>
        public System.Action OnDirtyBones;

        private void Reset()
        {
            puppet = this.GetComponent<HandPuppet>();
        }

        /// <summary>
        /// Relay to the Puppet to set the ghost hand to the desired static pose
        /// </summary>
        /// <param name="userPose">The static pose for the hand</param>
        /// <param name="relativeTo">The relative coordinates for the hand position/rotation</param>
        public void SetPose(HandPose userPose, Transform relativeTo)
        {
            puppet.LerpToPose(userPose, relativeTo, 1f, 1f);
        }

        /// <summary>
        /// Relay that extracts the current static pose of the hand in the desired coordinates system
        /// </summary>
        /// <param name="relativeTo">The object in which coordinates to represent the pose</param>
        /// <returns>The current static-pose of the ghost hand</returns>
        public HandPose ReadPose(Transform relativeTo)
        {
            return puppet.TrackedPose(relativeTo, true);
        }

        /// <summary>
        /// Ensures the stored data is kept up to date with the fingers transforms.
        /// </summary>
        private void LateUpdate()
        {
            if (AnyBoneChanged())
            {
                OnDirtyBones?.Invoke();
            }
        }

        /// <summary>
        /// Detects if we have moved the transforms of the fingers so the data can be kept up to date.
        /// To be used in Edit-mode.
        /// </summary>
        /// <returns>True if any of the fingers has moved from the previous frame.</returns>
        private bool AnyBoneChanged()
        {
            bool hasChanged = false;
            foreach (var bone in puppet.Bones)
            {
                if (bone.transform.hasChanged)
                {
                    bone.transform.hasChanged = false;
                    hasChanged = true;
                }
            }
            return hasChanged;
        }
    }
}
