using System.Reflection;
using UnityEngine;

namespace HandPosing.Interaction
{
    public class PhysicsGrabbable : Grabbable
    {
        [SerializeField]
        private Joint customJoint;

        private Joint _desiredJoint;
        private Joint _joint;

        protected override void Awake()
        {
            base.Awake();
            if (customJoint != null)
            {
                _desiredJoint = CopyCustomJoint(customJoint);
            }
        }

        public override void GrabBegin(BaseGrabber hand, Collider grabPoint)
        {
            base.GrabBegin(hand, grabPoint);

            if (_desiredJoint != null)
            {
                _joint = CloneJoint(_desiredJoint, this.gameObject) as Joint;
            }
            else
            {
                _joint = CreateDefaultJoint();
            }

            _joint.connectedBody = hand.GetComponent<Rigidbody>();
            _joint.autoConfigureConnectedAnchor = false;
            _joint.anchor = _joint.transform.InverseTransformPoint(hand.transform.position);
            _joint.connectedAnchor = Vector3.zero;

            _body.isKinematic = false;
        }

        public override void GrabEnd(Vector3 linearVelocity, Vector3 angularVelocity)
        {
            if (_joint != null)
            {
                Destroy(_joint);
                _joint = null;
            }

            base.GrabEnd(linearVelocity, angularVelocity);
        }

        public override void MoveTo(Vector3 desiredPos, Quaternion desiredRot) { }

        private Joint CreateDefaultJoint()
        {
            Joint joint = this.gameObject.AddComponent<FixedJoint>();
            joint.breakForce = Mathf.Infinity;
            return joint;
        }

        protected Joint CopyCustomJoint(Joint joint)
        {
            GameObject savedJointHolder = new GameObject();
            savedJointHolder.transform.SetParent(this.transform);
            Rigidbody body = savedJointHolder.AddComponent<Rigidbody>();
            body.isKinematic = true;
            CloneJoint(joint, savedJointHolder);
            savedJointHolder.name = "Saved Joint";
            savedJointHolder.SetActive(false);
            Destroy(joint);
            return savedJointHolder.GetComponent<Joint>();
        }

        public static Component CloneJoint(Joint joint, GameObject destination)
        {
            System.Type jointType = typeof(Joint);
            Component clone = destination.gameObject.AddComponent(joint.GetType());

            foreach (var foundProperty in joint.GetType().GetProperties())
            {
                if (foundProperty.DeclaringType.IsSubclassOf(jointType)
                    && foundProperty.CanWrite)
                {
                    foundProperty.SetValue(clone, foundProperty.GetValue(joint, null), null);
                }
            }
            return clone;
        }

    }
}