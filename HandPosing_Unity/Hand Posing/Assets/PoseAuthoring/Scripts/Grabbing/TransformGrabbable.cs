using UnityEngine;

namespace Interaction.Grabbables
{
    public class TransformGrabbable : Grabbable
    {
        public override void GrabBegin(Grabber hand, Collider grabPoint)
        {
            base.GrabBegin(hand, grabPoint);
            _body.isKinematic = true;
        }

        public override void MoveTo(Vector3 desiredPos, Quaternion desiredRot)
        {
            this.transform.position = desiredPos;
            this.transform.rotation = desiredRot;
        }
    }
}