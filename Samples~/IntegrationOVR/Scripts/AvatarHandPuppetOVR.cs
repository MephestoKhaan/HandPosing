using Oculus.Avatar;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HandPosing.OVRIntegration
{
    /// <summary>
    /// Script for moving Oculus Avatar hands with the puppeting system.
    /// Oculus Avatar hands are generated at runtime and only support controllers by default,
    /// with this script the pose can be overriden at runtime in first-person avatars.
    /// </summary>
    [DefaultExecutionOrder(20)]
    public class AvatarHandPuppetOVR : MonoBehaviour
    {
        [SerializeField]
        private OvrAvatar avatar;
        [SerializeField]
        private Transform handRoot;
        [SerializeField]
        private Transform handAnchor;
        [SerializeField]
        private Handeness handeness;

        private List<Transform> _joints;
        private ovrAvatarTransform[] _avatarTransforms;

        private OvrAvatarRenderComponent _avatarHandComponent;

        private void Awake()
        {
            _joints = new List<Transform>();
            ExtractJointsRecursive(this.handRoot, ref _joints);
        }

        private void LateUpdate()
        {
            FindHand();

            if (_avatarHandComponent != null)
            {
                _avatarHandComponent.gameObject.SetActive(true);
                _avatarHandComponent.mesh.enabled = true;

                JointsToAvatarTransforms(_joints, ref _avatarTransforms);

                if(handeness == Handeness.Right)
                {
                    CAPI.ovrAvatar_SetRightHandVisibility(avatar.sdkAvatar, true);
                    CAPI.ovrAvatar_SetRightHandCustomGesture(avatar.sdkAvatar, (uint)_avatarTransforms.Length, _avatarTransforms);
                }
                else
                {
                    CAPI.ovrAvatar_SetLeftHandVisibility(avatar.sdkAvatar, true);
                    CAPI.ovrAvatar_SetLeftHandCustomGesture(avatar.sdkAvatar, (uint)_avatarTransforms.Length, _avatarTransforms);
                }
            }
        }

        /// <summary>
        /// Searchs for the right handeness hand in the Avatar if available.
        /// </summary>
        private void FindHand()
        {
            if (_avatarHandComponent == null)
            {
                OvrAvatarHand avatarHand = handeness == Handeness.Right ? avatar.HandRight : avatar.HandLeft;
                _avatarHandComponent = avatarHand?.RenderParts[0];
            }
        }

        /// <summary>
        /// Extracts all the bones under a hand transform
        /// </summary>
        /// <param name="transform">The root transform from which to search for joints</param>
        /// <param name="joints">The shared Joints collection to be populated</param>
        private void ExtractJointsRecursive(Transform transform, ref List<Transform> joints)
        {
            joints.Add(transform);
            for (int i = 0; i < transform.childCount; ++i)
            {
                Transform child = transform.GetChild(i);
                ExtractJointsRecursive(child, ref joints);
            }
        }

        /// <summary>
        /// Converts a list of tracked finger bones to the Avatar trasform system
        /// </summary>
        /// <param name="joints">The list of finger bones, in the same order as the avatar</param>
        /// <param name="avatarTransforms">The bones converted in Avatar compatible instances</param>
        private void JointsToAvatarTransforms(List<Transform> joints, ref ovrAvatarTransform[] avatarTransforms)
        {
            if(avatarTransforms == null
                || avatarTransforms.Length != joints.Count)
            {
                avatarTransforms = new ovrAvatarTransform[joints.Count];
            }

            Pose anchorRelative = handAnchor.RelativeOffset(this.transform);
            avatarTransforms[0] = OvrAvatar.CreateOvrAvatarTransform(anchorRelative.position, anchorRelative.rotation);

            for (int i = 1; i < joints.Count; ++i)
            {
                Transform joint = joints[i];
                ovrAvatarTransform transform = OvrAvatar.CreateOvrAvatarTransform(joint.localPosition, joint.localRotation);
                avatarTransforms[i] = transform;
            }
        }
    }
}