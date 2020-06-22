using System;
using UnityEngine;

namespace PoseAuthoring.Grabbing
{
    public class Grabbable : MonoBehaviour
    {
        [SerializeField]
        protected Collider[] _grabPoints = null;

        protected bool _grabbedKinematic = false;
        protected Collider _grabbedCollider = null;
        protected Grabber _grabbedBy = null;


        public SnappableObject Snappable { get; private set; }

        public bool IsGrabbed
        {
            get
            {
                return _grabbedBy != null;
            }
        }

        public Grabber GrabbedBy
        {
            get
            {
                return _grabbedBy;
            }
        }

        public Rigidbody GrabbedBody
        {
            get
            {
                return _grabbedCollider.attachedRigidbody;
            }
        }

        public Collider[] GrabPoints
        {
            get
            {
                return _grabPoints;
            }
        }

        public virtual void GrabBegin(Grabber hand, Collider grabPoint)
        {
            _grabbedBy = hand;
            _grabbedCollider = grabPoint;
            gameObject.GetComponent<Rigidbody>().isKinematic = true;
        }


        public virtual void GrabEnd(Vector3 linearVelocity, Vector3 angularVelocity)
        {
            Rigidbody rb = gameObject.GetComponent<Rigidbody>();
            rb.isKinematic = _grabbedKinematic;
            rb.velocity = linearVelocity;
            rb.angularVelocity = angularVelocity;
            _grabbedBy = null;
            _grabbedCollider = null;
        }

        void Awake()
        {
            Snappable = this.GetComponent<SnappableObject>();


            Collider collider = this.GetComponentInChildren<Collider>();
            if (collider == null)
            {
                throw new ArgumentException("Grabbables cannot have zero grab points and no collider -- please add a grab point or collider.");
            }
            _grabPoints = new Collider[1] { collider };

        }


        protected virtual void Start()
        {
            _grabbedKinematic = this.GetComponent<Rigidbody>().isKinematic;
        }

        private void OnDisable()
        {
            UnsuscribeGrabber();
        }

        void OnDestroy()
        {
            UnsuscribeGrabber();
        }

        public void UnsuscribeGrabber()
        {
            Grabber.ClearAllGrabs(this);
        }
    }
}