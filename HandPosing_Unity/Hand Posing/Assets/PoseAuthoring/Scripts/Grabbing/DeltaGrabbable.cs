using UnityEngine;

namespace Interaction.Grabbables
{
    public class DeltaGrabbable : Grabbable
    {
        private Pose? desiredPhysicsPose;

        public override void GrabBegin(Grabber hand, Collider grabPoint)
        {
            base.GrabBegin(hand, grabPoint);
            _body.isKinematic = true;
        }

        public override void GrabEnd(Vector3 linearVelocity, Vector3 angularVelocity)
        {
            desiredPhysicsPose = null;
            base.GrabEnd(linearVelocity, angularVelocity);
        }

        public override void MoveTo(Vector3 desiredPos, Quaternion desiredRot)
        {
            desiredPhysicsPose = new Pose(desiredPos, desiredRot);
        }

        private void FixedUpdate()
        {
            if (desiredPhysicsPose.HasValue)
            {
                GrabbedBody.MoveRotation(desiredPhysicsPose.Value.rotation);
                GrabbedBody.MovePosition(desiredPhysicsPose.Value.position);
            }
        }
    }
}