using System.Reflection;
using UnityEngine;


namespace Interaction.Grabbables
{
    public class PhysicsGrabbable : Grabbable
    {
        [SerializeField]
        private Joint customJoint;

        private GameObject savedJointHolder;

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

        public override void GrabBegin(Grabber hand, Collider grabPoint)
        {
            base.GrabBegin(hand, grabPoint);

            if(_desiredJoint != null)
            {
                _joint = CloneComponent(_desiredJoint, this.gameObject) as Joint;
            }
            else
            {
                _joint = CreateDefaultJoint();
            }

            _joint.connectedBody = hand.GetComponent<Rigidbody>();
            _joint.autoConfigureConnectedAnchor = false;
            _joint.anchor = Vector3.zero;
            _joint.connectedAnchor = hand.transform.InverseTransformPoint(_joint.transform.position);

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

        public override void MoveTo(Vector3 desiredPos, Quaternion desiredRot){}

        private Joint CreateDefaultJoint()
        {
            Joint joint = this.gameObject.AddComponent<FixedJoint>();
            joint.breakForce = Mathf.Infinity;
            return joint;
        }

        protected Joint CopyCustomJoint(Joint joint)
        {
            savedJointHolder = new GameObject();
            savedJointHolder.transform.SetParent(this.transform);
            Rigidbody body = savedJointHolder.AddComponent<Rigidbody>();
            body.isKinematic = true;
            CloneComponent(joint, savedJointHolder);
            savedJointHolder.name = "Saved Joint";
            savedJointHolder.SetActive(false);
            Destroy(joint);
            return savedJointHolder.GetComponent<Joint>();
        }

        public static Component CloneComponent(Component source, GameObject destination)
        {
            Component tmpComponent = destination.gameObject.AddComponent(source.GetType());

            PropertyInfo[] foundProperties = source.GetType().GetProperties();
            for (int i = 0; i < foundProperties.Length; i++)
            {
                PropertyInfo foundProperty = foundProperties[i];
                if (foundProperty.CanWrite)
                {
                    foundProperty.SetValue(tmpComponent, foundProperty.GetValue(source, null), null);
                }
            }

            FieldInfo[] foundFields = source.GetType().GetFields();
            for (int i = 0; i < foundFields.Length; i++)
            {
                FieldInfo foundField = foundFields[i];
                foundField.SetValue(tmpComponent, foundField.GetValue(source));
            }
            return tmpComponent;
        }

    }
}