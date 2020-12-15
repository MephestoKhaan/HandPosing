using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HandPosing.Interaction
{
    public class Grabbable : MonoBehaviour
    {
        public Snappable Snappable { get; private set; }

        [SerializeField]
        protected bool immovable;


        private Collider[] _grabPoints = null;
        private bool _isKinematic = false;
        private HashSet<BaseGrabber> _grabbedBy = new HashSet<BaseGrabber>();
        protected Rigidbody _body;

        public bool IsGrabbed
        {
            get
            {
                return _grabbedBy != null;
            }
        }

        public Collider[] GrabPoints
        {
            get
            {
                return _grabPoints;
            }
        }

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

            Snappable = this.GetComponent<Snappable>();

            var colliders  = this.GetComponentsInChildren<Collider>().Where(c => !c.isTrigger);
            if (colliders == null
                || colliders.Count() == 0)
            {
                throw new ArgumentException("Grabbables cannot have zero grab points and no collider -- please add a grab point or collider.");
            }
            _grabPoints = colliders.ToArray();

        }

        private void OnDisable()
        {
            UnsuscribeGrabber();
        }

        void OnDestroy()
        {
            UnsuscribeGrabber();
        }

        public virtual void GrabBegin(BaseGrabber hand, Collider grabPoint)
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
        }

        public virtual void GrabEnd(BaseGrabber hand, Vector3 linearVelocity, Vector3 angularVelocity)
        {
            _body.isKinematic = _isKinematic;
            _body.velocity = linearVelocity;
            _body.angularVelocity = angularVelocity;

            if (_grabbedBy.Contains(hand))
            {
                _grabbedBy.Remove(hand);
            }
        }

        public virtual void MoveTo(Vector3 desiredPos, Quaternion desiredRot)
        {
            if(!immovable)
            {
                this.transform.position = desiredPos;
                this.transform.rotation = desiredRot;
            }
        }


        public void UnsuscribeGrabber()
        {
            BaseGrabber.ClearAllGrabs(this);
        }
    }
}