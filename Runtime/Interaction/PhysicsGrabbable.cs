using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

namespace HandPosing.Interaction
{
    public class PhysicsGrabbable : Grabbable
    {
        [SerializeField]
        private Joint customJoint;
        [SerializeField]
        private bool multiGrab;


        private Dictionary<BaseGrabber, Joint> _joints = new Dictionary<BaseGrabber, Joint>();

        protected override bool MultiGrab => multiGrab;

        private void OnValidate()
        {
            if (customJoint != null)
            {
                if (customJoint.gameObject == this.gameObject)
                {
                    Debug.LogError("Set the custom Joint to a disabled child GameObject");
                    GameObject holder = CreateJointHolder();
                    customJoint = CloneJoint(customJoint, holder);
                }
                else
                {
                    customJoint.gameObject.SetActive(false);
                }
            }
        }

        public override void GrabBegin(BaseGrabber hand, Collider grabPoint)
        {
            base.GrabBegin(hand, grabPoint);

            if (immovable)
            {
                return;
            }


            Joint joint = null;
            if (customJoint != null)
            {
                joint = CloneJoint(customJoint, this.gameObject) as Joint;
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

        protected GameObject CreateJointHolder()
        {
            GameObject savedJointHolder = new GameObject();
            savedJointHolder.name = "Saved Joint";
            savedJointHolder.SetActive(false);
            savedJointHolder.transform.SetParent(this.transform);
            Rigidbody body = savedJointHolder.AddComponent<Rigidbody>();
            body.isKinematic = true;
            return savedJointHolder;
        }

        private static Joint CloneJoint(Joint joint, GameObject destination)
        {
            System.Type jointType = typeof(Joint);
            Joint clone = destination.gameObject.AddComponent(joint.GetType()) as Joint;

            foreach (var foundProperty in joint.GetType().GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance))
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