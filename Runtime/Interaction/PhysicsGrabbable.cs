using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

namespace HandPosing.Interaction
{
    /// <summary>
    /// This custom version of the Grabbable, uses physic Joints instead of transforms to 
    /// move the object.
    /// 
    /// It support setting a custom joint for the grab, which can be specially useful when
    /// the grabbable has some constraints (a lever, a turning wheel) as it does not need 
    /// to follow exactly the position of the hand.
    /// </summary>
    public class PhysicsGrabbable : Grabbable
    {
        /// <summary>
        /// The custom joint to use when grabbing this grabbable.
        /// Set this field to a Joint in an disabled gameObject, it will be duplicated everytime
        /// a new Grabber grabs this object, and the anchoring will be adjusted automatically.
        /// 
        /// Not mandatory, if not set a FixedJoint will be used. 
        /// If the grabbable is already attached to something with a joint, using PreProcessing here is not recommended.
        /// </summary>
        [SerializeField]
        [Tooltip("Not mandatory. Specify a custom joint to mimic when grabbing, must be disabled.")]
        private ConfigurableJoint customJoint;

        /// <summary>
        /// Using physics, multiple grabbers can held the object. 
        /// Set to true if this is the desired behaviour instead of swapping hands.
        /// </summary>
        [SerializeField]
        [Tooltip("Allow multiple grabbers to held the object.")]
        private bool multiGrab;

        private Dictionary<BaseGrabber, Joint> _joints = new Dictionary<BaseGrabber, Joint>();

        protected override bool MultiGrab => multiGrab;

        /// <summary>
        /// Ensure the Custom Joint is set to a disabled GameObject, as it will be used
        /// just to read the values and duplicate them onto a new dynamically generated joint.
        /// </summary>
        private void OnValidate()
        {
            if (customJoint != null)
            {
                if (customJoint.gameObject == this.gameObject)
                {
                    Debug.LogError($"Set the custom Configurable Joint at {this.name} to a disabled child GameObject", this.gameObject);
                    GameObject holder = CreateJointHolder();
                    customJoint = CloneJoint(customJoint, holder);
                }
                else
                {
                    customJoint.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// When the grab begins, create the joint that will held the object to the grabber.
        /// The Anchor points of the Joint specify the offset to the grabber here.
        /// If a custom joint was provided, duplicate it and mimic its values, if not it will use a fixe joint.
        /// </summary>
        /// <param name="hand">The grabber hand</param>
        public override void GrabBegin(BaseGrabber hand)
        {
            base.GrabBegin(hand);

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

        /// <summary>
        /// When the object is released, remove the joint associated to the hand.
        /// </summary>
        /// <param name="hand">Hand that released the object.</param>
        /// <param name="linearVelocity">Linear velocity of the throw.</param>
        /// <param name="angularVelocity">Angular velocity of the throw.</param>
        public override void GrabEnd(BaseGrabber hand, Vector3 linearVelocity, Vector3 angularVelocity)
        {
            RemoveJoint(hand);
            base.GrabEnd(hand, linearVelocity, angularVelocity);
        }

        /// <summary>
        /// Since Joints are automatically holding the grabbable to the grabber, this method is empty.
        /// </summary>
        /// <param name="desiredPos">The desired position of the grabbable.</param>
        /// <param name="desiredRot">The desired rotation of the grabbable.</param>
        public override void MoveTo(Vector3 desiredPos, Quaternion desiredRot) { }


        private void RemoveJoint(BaseGrabber hand)
        {
            if (_joints.TryGetValue(hand, out Joint joint))
            {
                _joints.Remove(hand);
                Destroy(joint);
            }
        }

        /// <summary>
        /// When no CustomJoint is specified, this Joint is used.
        /// An unbreakable Fixed Joint without Preprocessing.
        /// </summary>
        /// <returns></returns>
        private Joint CreateDefaultJoint()
        {
            Joint joint = this.gameObject.AddComponent<FixedJoint>();
            joint.breakForce = Mathf.Infinity;
            joint.enablePreprocessing = false;
            return joint;
        }

        /// <summary>
        /// Creates a disabled GameObject for holding the data of the desired Custom Joint.
        /// </summary>
        /// <returns>A children GameObject for holding Joint data.</returns>
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

        /// <summary>
        /// Copy all the properties of a ConfigurableJoint onto a new one.
        /// </summary>
        /// <param name="joint">The Joint to be copied.</param>
        /// <param name="destination">The GameObject that will contain the new Joint.</param>
        /// <returns>The created Joint.</returns>
        private static ConfigurableJoint CloneJoint(ConfigurableJoint joint, GameObject destination)
        {
            ConfigurableJoint clone = destination.gameObject.AddComponent<ConfigurableJoint>();

            //From Joint
            clone.connectedBody = joint.connectedBody;
            clone.axis = joint.axis;
            clone.anchor = joint.anchor;
            clone.connectedAnchor = joint.connectedAnchor;
            clone.autoConfigureConnectedAnchor = joint.autoConfigureConnectedAnchor;
            clone.breakForce = joint.breakForce;
            clone.breakTorque = joint.breakTorque;
            clone.enableCollision = joint.enableCollision;
            clone.enablePreprocessing = joint.enablePreprocessing;
            clone.massScale = joint.massScale;
            clone.connectedMassScale = joint.connectedMassScale;
            ///From ConfigurableJoint
            clone.projectionAngle = joint.projectionAngle;
            clone.projectionDistance = joint.projectionDistance;
            clone.projectionMode = joint.projectionMode;
            clone.slerpDrive = joint.slerpDrive;
            clone.angularYZDrive = joint.angularYZDrive;
            clone.angularXDrive = joint.angularXDrive;
            clone.rotationDriveMode = joint.rotationDriveMode;
            clone.targetAngularVelocity = joint.targetAngularVelocity;
            clone.targetRotation = joint.targetRotation;
            clone.zDrive = joint.zDrive;
            clone.yDrive = joint.yDrive;
            clone.xDrive = joint.xDrive;
            clone.targetVelocity = joint.targetVelocity;
            clone.targetPosition = joint.targetPosition;
            clone.angularZLimit = joint.angularZLimit;
            clone.angularYLimit = joint.angularYLimit;
            clone.highAngularXLimit = joint.highAngularXLimit;
            clone.lowAngularXLimit = joint.lowAngularXLimit;
            clone.linearLimit = joint.linearLimit;
            clone.angularYZLimitSpring = joint.angularYZLimitSpring;
            clone.angularXLimitSpring = joint.angularXLimitSpring;
            clone.linearLimitSpring = joint.linearLimitSpring;
            clone.angularZMotion = joint.angularZMotion;
            clone.angularYMotion = joint.angularYMotion;
            clone.angularXMotion = joint.angularXMotion;
            clone.zMotion = joint.zMotion;
            clone.yMotion = joint.yMotion;
            clone.xMotion = joint.xMotion;
            clone.secondaryAxis = joint.secondaryAxis;
            clone.configuredInWorldSpace = joint.configuredInWorldSpace;
            clone.swapBodies = joint.swapBodies;

            return clone;
        }

    }
}