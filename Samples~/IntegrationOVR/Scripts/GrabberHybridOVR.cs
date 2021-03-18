﻿using HandPosing.Interaction;
using UnityEngine;
using HandPosing.OVRIntegration.GrabEngine;

namespace HandPosing.OVRIntegration
{
    /// <summary>
    /// Custom grabber for the Oculus Plugin
    /// This Grabber supports grabbing with both Oculus Hand tracking, using Pinch gesture
    /// and grabbing using Oculus Touch controllers, using the Primary Hand Trigger.
    /// </summary>
    public class GrabberHybridOVR : BaseGrabber
    {
        [SerializeField]
        [Tooltip("Provide the grabbing interfaces in priority order, if the first one is not valid it will fallback to the next one")]
        private Component[] flexInterfaces;

        private Vector3 _prevPosition;
        private Quaternion _prevRotation;

        private Vector3 _velocity;
        private Vector3 _angularVelocity;

        private static readonly FlexInterface FLEX_NULL = new NoopFlex();

        public FlexInterface Flex
        {
            get
            {
                for (int i = 0; i < flexInterfaces.Length; i++)
                {
                    FlexInterface flex = flexInterfaces[i] as FlexInterface;
                    if (flex.IsValid)
                    {
                        return flex;
                    }
                }
                return FLEX_NULL;
            }
        }

        private const float VELOCITY_DAMPING = 20f;

        protected override void Grab(Grabbable closestGrabbable)
        {
            base.Grab(closestGrabbable);

            if (GrabbedObject != null)
            {
                _prevPosition = GrabbedObject.transform.position;
                _prevRotation = GrabbedObject.transform.rotation;
            }
        }

        protected void LateUpdate()
        {
            if (GrabbedObject != null)
            {
                UpdateVelocity(GrabbedObject.transform);
            }
        }

        private void UpdateVelocity(Transform relativeTo)
        {
            Vector3 instantVelocity = (relativeTo.position - _prevPosition) / Time.deltaTime;
            Quaternion deltaRotation = relativeTo.rotation * Quaternion.Inverse(_prevRotation);
            float theta = 2.0f * Mathf.Acos(Mathf.Clamp(deltaRotation.w, -1.0f, 1.0f));
            if (theta > Mathf.PI)
            {
                theta -= 2.0f * Mathf.PI;
            }
            Vector3 angularVelocity = new Vector3(deltaRotation.x, deltaRotation.y, deltaRotation.z).normalized * theta / Time.deltaTime;

            _velocity = Vector3.Lerp(instantVelocity, _velocity, Time.deltaTime * VELOCITY_DAMPING);
            _angularVelocity = Vector3.Lerp(angularVelocity, _angularVelocity, Time.deltaTime * VELOCITY_DAMPING);

            _prevPosition = relativeTo.position;
            _prevRotation = relativeTo.rotation;
        }

        public override float CurrentFlex() => Flex.GrabStrength??0f;
        public override Vector2 GrabFlexThreshold => Flex.GrabThreshold;
        public override Vector2 GrabAttemptThreshold => Flex.GrabAttemptThreshold;
        public override float ReleasedFlexThreshold => Flex.AlmostGrabRelease;

        protected override (Vector3, Vector3) HandRelativeVelocity(Pose offsetPose) => (_velocity, _angularVelocity);
    }
}