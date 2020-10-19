using PoseAuthoring;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Interaction
{
    public class Grabbable : MonoBehaviour
    {
        public SnappableObject Snappable { get; private set; }

        private HashSet<GameObject> _colliderObjects = null;

        private Collider[] _grabPoints = null;
        private bool _isKinematic = false;
        private Collider _grabbedCollider = null;
        private Grabber _grabbedBy = null;
        protected Rigidbody _body;


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

            _body.isKinematic = true;
        }

        public virtual void GrabEnd(Vector3 linearVelocity, Vector3 angularVelocity)
        {
            _body.isKinematic = _isKinematic;
            _body.velocity = linearVelocity;
            _body.angularVelocity = angularVelocity;


            _grabbedBy = null;
            _grabbedCollider = null;
        }

        public virtual void MoveTo(Vector3 desiredPos, Quaternion desiredRot)
        {
            this.transform.position = desiredPos;
            this.transform.rotation = desiredRot;
        }

        protected virtual void Awake()
        {
            _body = this.GetComponent<Rigidbody>();
            _isKinematic = _body.isKinematic;

            Snappable = this.GetComponent<SnappableObject>();

            PopulateColliderObjects();

            Collider collider = this.GetComponentInChildren<Collider>();
            if (collider == null)
            {
                throw new ArgumentException("Grabbables cannot have zero grab points and no collider -- please add a grab point or collider.");
            }
            _grabPoints = new Collider[1] { collider };

        }

        private void PopulateColliderObjects()
        {
            var colliders = this.GetComponentsInChildren<Collider>().Where(c => !c.isTrigger);
            _colliderObjects = new HashSet<GameObject>();
            foreach (var col in colliders)
            {
                if (!_colliderObjects.Contains(col.gameObject))
                {
                    _colliderObjects.Add(col.gameObject);
                }
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

        public void UnsuscribeGrabber()
        {
            Grabber.ClearAllGrabs(this);
        }
    }
}