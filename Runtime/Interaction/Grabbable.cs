using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HandPosing.Interaction
{
    /// <summary>
    /// This class specifies a basic grabbable object. Taking care of moving the object while
    /// beign held by a BaseGrabber using local offsets (similar to parenting transforms).
    /// 
    /// Inherit this class to implement your own approach (like PHysicsGrabbable that uses joints).
    /// Or completely create your own Grabber-Grabbable system by implementing the IGrabNotifier in your custom grabber.
    /// </summary>
    public class Grabbable : MonoBehaviour
    {
        /// <summary>
        /// Set to true to avoid the Grabber to move this grabbable
        /// </summary>
        [SerializeField]
        [Tooltip("Set to true to avoid the Grabber to move this grabbable")]
        protected bool immovable;

        /// <summary>
        /// List of colliders elligible for grabbing.
        /// Not mandatory. If null it will be autopopulated to all the children colliders.
        /// </summary>
        [SerializeField]
        [Tooltip("Not mandatory. Colliders eligible for grabbing.")]
        private Collider[] _grabPoints = null;

        private bool _isKinematic = false;
        private HashSet<BaseGrabber> _grabbedBy = new HashSet<BaseGrabber>();
        protected Rigidbody _body;

        /// <summary>
        /// Event called when the object is grabbed
        /// </summary>
        public Action<BaseGrabber> OnGrabbed;
        /// <summary>
        /// Event called when the object is released
        /// </summary>
        public Action<BaseGrabber> OnReleased;


        /// <summary>
        /// True if the object is being held by a Grabber.
        /// </summary>
        public bool IsGrabbed
        {
            get
            {
                return _grabbedBy.Count > 0;
            }
        }

        /// <summary>
        /// General getter for the colliders eligible for grabbing.
        /// </summary>
        public Collider[] GrabPoints
        {
            get
            {
                return _grabPoints;
            }
        }

        /// <summary>
        /// True if the object can be held by multiple hands. 
        /// Note: here not implemented, but supported by the class PhysicsGrabbable instead
        /// </summary>
        protected virtual bool MultiGrab
        {
            get
            {
                return false;
            }
        }

        protected virtual void Awake()
        {
            _body = this.GetComponent<Rigidbody>();
            _isKinematic = _body.isKinematic;

            if (_grabPoints == null || _grabPoints.Length == 0)
            {
                var colliders = this.GetComponentsInChildren<Collider>().Where(c => !c.isTrigger);
                if (colliders == null
                    || colliders.Count() == 0)
                {
                    throw new ArgumentException("Grabbables cannot have zero grab points and no collider -- please add a grab point or collider.");
                }
                _grabPoints = colliders.ToArray();
            }

        }

        private void OnDisable()
        {
            UnsuscribeGrabber();
        }

        void OnDestroy()
        {
            UnsuscribeGrabber();
        }

        /// <summary>
        /// When the object is grabbed, record the grabber and disable physics.
        /// </summary>
        /// <param name="hand">Grabber hand.</param>
        public virtual void GrabBegin(BaseGrabber hand)
        {
            if(!MultiGrab)
            {
                foreach(var grabber in _grabbedBy.ToList())
                {
                    grabber.OffhandGrabbed(this);
                }
                _grabbedBy.Clear();
            }

            if(!_grabbedBy.Contains(hand))
            {
                _grabbedBy.Add(hand);
            }
            _body.isKinematic = true;

            OnGrabbed?.Invoke(hand);
        }

        /// <summary>
        /// Restore the GrabStart values when object is released and throw it if not being held.
        /// </summary>
        /// <param name="hand"></param>
        /// <param name="linearVelocity"></param>
        /// <param name="angularVelocity"></param>
        public virtual void GrabEnd(BaseGrabber hand, Vector3 linearVelocity, Vector3 angularVelocity)
        {
            if (_grabbedBy.Contains(hand))
            {
                _grabbedBy.Remove(hand);
            }
            if(_grabbedBy.Count == 0)
            {
                _body.isKinematic = _isKinematic;
                _body.velocity = linearVelocity;
                _body.angularVelocity = angularVelocity;
            }

            OnReleased?.Invoke(hand);
        }

        /// <summary>
        /// Move the object to the specified position and rotation.
        /// This is called everytime the grabber moves.
        /// </summary>
        /// <param name="desiredPos">Desired object world position.</param>
        /// <param name="desiredRot">Desired object world rotation.</param>
        public virtual void MoveTo(Vector3 desiredPos, Quaternion desiredRot)
        {
            if(!immovable)
            {
                this.transform.position = desiredPos;
                this.transform.rotation = desiredRot;
            }
        }

        /// <summary>
        /// Clear all grabs on this object.
        /// </summary>
        public void UnsuscribeGrabber()
        {
            BaseGrabber.ClearAllGrabs(this);
        }
    }
}