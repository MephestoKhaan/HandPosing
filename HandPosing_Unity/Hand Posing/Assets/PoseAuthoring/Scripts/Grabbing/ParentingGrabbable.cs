using UnityEngine;


namespace Interaction.Grabbables
{
    public class ParentingGrabbable : Grabbable
    {
        private Transform _oldParent;

        public override void GrabBegin(Grabber hand, Collider grabPoint)
        {
            base.GrabBegin(hand, grabPoint);
            _oldParent = this.transform.parent;
            this.transform.SetParent(hand.transform);
        }

        public override void GrabEnd(Vector3 linearVelocity, Vector3 angularVelocity)
        {
            base.GrabEnd(linearVelocity, angularVelocity);
            this.transform.SetParent(_oldParent);
        }
    }
}