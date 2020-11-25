using Oculus.Avatar;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HandPosing.OVRIntegration
{
    public class AvatarHandPuppetOVR : MonoBehaviour
    {
        [SerializeField]
        private OvrAvatar avatar;
        [SerializeField]
        private Transform anchor;
        [SerializeField]
        private Transform handRoot;
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

        private void FindHand()
        {
            if (_avatarHandComponent == null)
            {
                OvrAvatarHand avatarHand = handeness == Handeness.Right ? avatar.HandRight : avatar.HandLeft;
                _avatarHandComponent = avatarHand?.RenderParts[0];
            }
        }

        private void ExtractJointsRecursive(Transform transform, ref List<Transform> joints)
        {
            joints.Add(transform);
            for (int i = 0; i < transform.childCount; ++i)
            {
                Transform child = transform.GetChild(i);
                ExtractJointsRecursive(child, ref joints);
            }
        }

        private void JointsToAvatarTransforms(List<Transform> joints, ref ovrAvatarTransform[] avatarTransforms)
        {
            if(avatarTransforms == null
                || avatarTransforms.Length != joints.Count)
            {
                avatarTransforms = new ovrAvatarTransform[joints.Count];
            }

            Pose localRoot = anchor.RelativeOffset(handRoot);
            avatarTransforms[0] = OvrAvatar.CreateOvrAvatarTransform(localRoot.position, localRoot.rotation);

            for (int i = 1; i < joints.Count; ++i)
            {
                Transform joint = joints[i];
                ovrAvatarTransform transform = OvrAvatar.CreateOvrAvatarTransform(joint.localPosition, joint.localRotation);
                avatarTransforms[i] = transform;
            }
        }
    }
}