using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace HandPosing.Interaction
{
    public class PhysicsGrabbable : Grabbable
    {
        [SerializeField]
        private Joint customJoint;
        [SerializeField]
        private bool multiGrab;

        private Joint _desiredJoint;
        private Dictionary<BaseGrabber, Joint> _joints = new Dictionary<BaseGrabber, Joint>();

        protected override bool MultiGrab => multiGrab;

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

            if(immovable)
            {
                return;
            }


            Joint joint = null;
            if (_desiredJoint != null)
            {
                joint = CloneJoint(_desiredJoint, this.gameObject) as Joint;
            }
            else
            {
                joint = CreateDefaultJoint();
            }

            joint.connectedBody = hand.GetComponent<Rigidbody>();
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = joint.transform.InverseTransformPoint(hand.transform.position);
            joint.connectedAnchor = Vector3.zero;

            RemoveJoint(hand);
            _joints.Add(hand, joint);

            _body.isKinematic = false;
        }

        public override void GrabEnd(BaseGrabber hand, Vector3 linearVelocity, Vector3 angularVelocity)
        {
            RemoveJoint(hand);

            base.GrabEnd(hand, linearVelocity, angularVelocity);
        }


        public override void MoveTo(Vector3 desiredPos, Quaternion desiredRot) { }

        private void RemoveJoint(BaseGrabber hand)
        {
            if (_joints.TryGetValue(hand, out Joint joint))
            {
                _joints.Remove(hand);
                Destroy(joint);
            }
        }

        private Joint CreateDefaultJoint()
        {
            Joint joint = this.gameObject.AddComponent<FixedJoint>();
            joint.breakForce = Mathf.Infinity;
            joint.enablePreprocessing = false;
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

        private static Component CloneJoint(Joint joint, GameObject destination)
        {
            System.Type jointType = typeof(Joint);
            Component clone = destination.gameObject.AddComponent(joint.GetType());

            PropertyInfo[] properties = joint.GetType().GetProperties(
                BindingFlags.FlattenHierarchy
                | BindingFlags.Public
                | BindingFlags.Instance);

            foreach (var foundProperty in properties)
            {
                if ((foundProperty.DeclaringType.IsSubclassOf(jointType)
                    || foundProperty.DeclaringType == jointType)
                    && foundProperty.CanWrite)
                {
                    foundProperty.SetValue(clone, foundProperty.GetValue(joint, null), null);
                }
            }
            return clone;
        }

    }
}