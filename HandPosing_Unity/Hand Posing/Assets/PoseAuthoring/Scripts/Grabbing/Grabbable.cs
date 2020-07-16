using PoseAuthoring;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Interaction
{
    public class Grabbable : MonoBehaviour
    {
        [SerializeField]
        private bool _canMove = true;
        [SerializeField]
        private bool _physicsMove = false;

        public SnappableObject Snappable { get; private set; }

        private HashSet<GameObject> _colliderObjects = null;

        private Collider[] _grabPoints = null;
        private bool _isKinematic = false;
        private Collider _grabbedCollider = null;
        private Grabber _grabbedBy = null;

        private (Vector3, Quaternion)? desiredPhysicsPose;
        

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

        public bool CanMove
        {
            get
            {
                return _canMove;
            }
        }
        public bool PhysicsMove
        {
            get
            {
                return _physicsMove;
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
            if(!PhysicsMove)
            {
                gameObject.GetComponent<Rigidbody>().isKinematic = true;
            }
        }


        public virtual void GrabEnd(Vector3 linearVelocity, Vector3 angularVelocity)
        {
            if(GrabbedBody != null)
            {
                Rigidbody rb = GrabbedBody;
                rb.isKinematic = _isKinematic;
                rb.velocity = linearVelocity;
                rb.angularVelocity = angularVelocity;
            }
            _grabbedBy = null;
            _grabbedCollider = null;
            desiredPhysicsPose = null;
        }


        public virtual void MoveTo(Vector3 desiredPos, Quaternion desiredRot)
        {
            if(!CanMove)
            {
                return;
            }

            if (PhysicsMove)
            {
                desiredPhysicsPose = (desiredPos, desiredRot);
            }
            else
            {
                this.transform.position = desiredPos;
                this.transform.rotation = desiredRot;
            }
        }

        void Awake()
        {
            Snappable = this.GetComponent<SnappableObject>();

            PopulateColliderObjects();

            Collider collider = this.GetComponentInChildren<Collider>();
            if (collider == null)
            {
                throw new ArgumentException("Grabbables cannot have zero grab points and no collider -- please add a grab point or collider.");
            }
            // Create a default grab point
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

        private void LateUpdate()
        {
            if(desiredPhysicsPose.HasValue && GrabbedBody != null)
            {
                GrabbedBody.MovePosition(desiredPhysicsPose.Value.Item1);
                GrabbedBody.MoveRotation(desiredPhysicsPose.Value.Item2);
               
            }
        }

        protected virtual void Start()
        {
            _isKinematic = this.GetComponent<Rigidbody>().isKinematic;
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